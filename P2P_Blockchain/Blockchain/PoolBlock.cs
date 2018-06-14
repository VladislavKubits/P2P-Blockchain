using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.Configuration;

namespace P2P_Blockchain.Blockchain
{
    class PoolBlock
    {
        public int ID { get; set; }
        public string Hash { get; set; }
        public string Data { get; set; }
        public string HashList { get; set; }

        public PoolBlock() {}

        public PoolBlock(int ID, string Hash, string Data, string HashList)
        {
            this.ID = ID;
            this.Hash = Hash;
            this.Data = Data;
            this.HashList = HashList;
        }

        public static DataTable SelectToConnect(OleDbConnection connection, string SQLCMD)
        {
            connection.Open();

            OleDbCommand command = connection.CreateCommand();
            command.CommandText = SQLCMD;
            OleDbDataAdapter adapter = new OleDbDataAdapter(command);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            connection.Close();
            return dataTable;

        }


        public static void EditToConnect(OleDbConnection connection, string SQLCMD)
        {
            connection.Open();

            OleDbCommand command = connection.CreateCommand();
            command.CommandText = SQLCMD;
            OleDbTransaction transaction = connection.BeginTransaction();
            command.Transaction = transaction;
            command.ExecuteNonQuery();

            transaction.Commit();
            connection.Close();

        }


        public static bool MakePoolBlock(OleDbConnection connection, Transaction[] transactions, string privateKey, string publicKey)
        {
            Transaction pool = new Transaction();
            PoolBlock blockList = new PoolBlock();
            Timer timer = new Timer(10000);
            timer.Elapsed += (sender, e) => TimerTick(sender, e, out blockList, transactions, timer, out timer);
            blockList.Hash = null;
            blockList.HashList = null;
            timer.Enabled = true;
            while (true)
            {
                transactions = pool.GetTransaction(connection);
                if (transactions.Count() >= 20)
                {
                    timer.Enabled = false;
                    timer.Dispose();
                    for (int i = 0; i < 20; i++)
                    {
                        blockList.Hash += transactions[i].Hash;
                        blockList.HashList += (i < 19) ? transactions[i].Hash + ":" : transactions[i].Hash;
                    }
                    break;
                }
                if(!timer.Enabled && transactions.Count() == 0)
                {
                    Console.Write("\nНет транзакций\n{0}> ", ConfigurationManager.AppSettings["username"]);
                    timer.Enabled = true;
                }
                else if (!timer.Enabled && transactions.Count() != 0)
                {
                    timer.Dispose();
                    break;
                }
                System.Threading.Thread.Sleep(500);
            }
            blockList.Hash = RSA.GetMd5Hash(blockList.Hash);

            string MD5PK = RSA.GetMd5Hash(publicKey);


            string Hash = blockList.Hash;
            string SignedHash = Convert.ToBase64String(RSA.SignData(RSA.hashedText(blockList.Hash), privateKey));
            string HashList = blockList.HashList;



            //Создание таблицы с названием которо хранитьсяв Hash
            string SQLCMD = "CREATE TABLE " + Hash + "([ID] INT NOT NULL PRIMARY KEY, [Sender] VARCHAR(255) NULL, [Level] VARCHAR(255) NULL, [Hash] VARCHAR(255) NULL, [SignedHash] TEXT NULL, [Data] TEXT NULL)";
            EditToConnect(connection, SQLCMD);

            string[] HashList_array = Regex.Split(HashList, ":");

            SQLCMD = "SELECT * FROM Transaction WHERE";

            for (int i = 0; i < HashList_array.Count(); i++)
            {
                SQLCMD += " [Hash] = '" + HashList_array[i] + "'";

                if (i != HashList_array.Count()-1)
                {
                    SQLCMD = SQLCMD + " OR";
                }
            }

            DataTable TableTransacationOnBlock = SelectToConnect(connection, SQLCMD);

            Transaction[] transaction = new Transaction[TableTransacationOnBlock.Rows.Count];
            for(int i = 0; i < TableTransacationOnBlock.Rows.Count; i++)
            {
                transaction[i] = new Transaction(Convert.ToInt32(TableTransacationOnBlock.Rows[i]["ID"]), TableTransacationOnBlock.Rows[i]["Sender"].ToString(),
                    TableTransacationOnBlock.Rows[i]["Level"].ToString(), TableTransacationOnBlock.Rows[i]["Hash"].ToString(),
                    TableTransacationOnBlock.Rows[i]["SignedHash"].ToString(), TableTransacationOnBlock.Rows[i]["Data"].ToString());
            }


            //Запись данных в тело блока (тело блока создано выше)
            for (int i = 0; i < transaction.Count(); i++)
            {
                SQLCMD = String.Format("INSERT INTO {0} (ID, Sender, Level, Hash, SignedHash, Data) VALUES ('{1}', '{2}', '{3}', '{4}', '{5}', '{6}')",
                    Hash, transaction[i].ID, transaction[i].Sender, transaction[i].Level, transaction[i].Hash, transaction[i].SignedHash, transaction[i].Data);
                EditToConnect(connection, SQLCMD);
            }

            //Удаление группы транзакций по списку хэшей
            SQLCMD = "DELETE * FROM Transaction WHERE";

            for (int i = 0; i < HashList_array.Count(); i++)
            {
                SQLCMD += " [Hash] = '" + HashList_array[i] + "'";

                if (i != HashList_array.Count()-1)
                {
                    SQLCMD += " OR";
                }
            }

            EditToConnect(connection, SQLCMD);

            blockList.Data += String.Format("{0}:{1};", SignedHash, MD5PK);

            try
            {
                connection.Open();

                OleDbCommand command = connection.CreateCommand();
                command.CommandText = string.Format("INSERT INTO PoolBlock (Hash, Data, HashList) VALUES ('{0}', '{1}', '{2}')", blockList.Hash, blockList.Data, blockList.HashList);

                OleDbTransaction trans = connection.BeginTransaction();

                command.Transaction = trans;
                command.ExecuteNonQuery();
                trans.Commit();

                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }

        private static void TimerTick(object sender, ElapsedEventArgs e, out PoolBlock listOut, Transaction[] transactions, Timer timer,
            out Timer timerOut)
        {
            PoolBlock blockList = new PoolBlock();
            for (int i = 0; i < transactions.Count(); i++)
            {
                blockList.Hash += transactions[i].Hash;
                blockList.HashList += (i < transactions.Count() - 1) ? transactions[i].Hash + ":" : transactions[i].Hash;
            }
            listOut = blockList;
            timerOut = timer;
            timerOut.Enabled = false;
        }

        public static void SetSignature(OleDbConnection connection, string privateKey, string publicKey)
        {

            DataTable dataTable = PoolBlock.SelectToConnect(connection, "SELECT * FROM PoolBlock");

            PoolBlock[] poolBlockLists = new PoolBlock[dataTable.Rows.Count];

            for (int i =0; i < dataTable.Rows.Count; i++)
            {
                poolBlockLists[i] = new PoolBlock(Convert.ToInt32(dataTable.Rows[i]["ID"]), dataTable.Rows[i]["Hash"].ToString(), dataTable.Rows[i]["Data"].ToString(), 
                    dataTable.Rows[i]["HashList"].ToString());
            }

            foreach (PoolBlock poolBlockList in poolBlockLists)
            {
                string[] arr_string = Regex.Split(poolBlockList.Data, "/?[:;]");

                bool flag = false;

                string MD5PK = RSA.GetMd5Hash(publicKey);
                string SignedHash = Convert.ToBase64String(RSA.SignData(RSA.hashedText(poolBlockList.Hash), privateKey));

                foreach (string str in arr_string)
                {
                    if (str == MD5PK)
                    {
                        flag = true;
                        break;
                    }
                }

                if ((arr_string.Count() - 1) == 4 && flag == false)
                {
                    poolBlockList.Data += String.Format("{0}:{1};", SignedHash, MD5PK);
                    EditToConnect(connection, String.Format("UPDATE PoolBlock SET Data = '{0}' WHERE [Hash] = '{1}'", poolBlockList.Data, poolBlockList.Hash));
                    Block block = new Block();
                    block.MakeBlock(connection, poolBlockList.Hash);
                }
                else if ((arr_string.Count()-1) <= 2 && flag == false)
                {
                    poolBlockList.Data += String.Format("{0}:{1};", SignedHash, MD5PK);
                    EditToConnect(connection, String.Format("UPDATE PoolBlock SET Data = '{0}' WHERE [Hash] = '{1}'", poolBlockList.Data, poolBlockList.Hash));
                }
            }
        }
    }
}
