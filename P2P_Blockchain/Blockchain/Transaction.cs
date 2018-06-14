using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;

namespace P2P_Blockchain.Blockchain
{
    class Transaction
    {
        public int ID { get; set; }
        public string Sender { get; set; }
        public string Level { get; set; }
        public string Hash { get; set; }
        public string SignedHash { get; set; }
        public string Data { get; set; }

        public Transaction() { }

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


        public Transaction(int ID, string Sender, string Level, string Hash, string SignedHash, string Data)
        {
            this.ID = ID;
            this.Sender = Sender;
            this.Level = Level;
            this.Hash = Hash;
            this.SignedHash = SignedHash;
            this.Data = Data;
        }

        public bool SetTransaction(OleDbConnection connection, string privateKey, string publicKey, string Level, string Data)
        {
            Sender = RSA.GetMd5Hash(publicKey);
            Hash = RSA.GetMd5Hash(Sender + Level + Data);
            SignedHash = Convert.ToBase64String(RSA.SignData(RSA.hashedText(Hash), privateKey));
            try
            {
                string CMD = string.Format("INSERT INTO Transaction (Sender, Level, Hash, SignedHash, Data) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", Sender, Level, Hash, SignedHash, Data);
                CMD = Convert.ToString(CMD);
                EditToConnect(connection, CMD);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public bool DeleteRow(OleDbConnection connection, string column, string value)
        {
            try
            {
                connection.Open();

                OleDbCommand command = connection.CreateCommand();
                command.CommandText = string.Format("DELETE FROM Transaction WHERE [{0}] = '{1}'", column, value);

                OleDbTransaction transaction = connection.BeginTransaction();
                command.Transaction = transaction;
                command.ExecuteNonQuery();

                transaction.Commit();
                connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public Transaction[] GetTransaction(OleDbConnection connection)
        {
            try
            {
                connection.Open();

                OleDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Transaction";
                OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                connection.Close();

                Transaction[] transaction = new Transaction[dataTable.Rows.Count];
                int rowCount = 0;
                foreach (DataRow row in dataTable.Rows)
                {
                    transaction[rowCount] = new Transaction(Convert.ToInt32(row["ID"]), row["Sender"].ToString(), row["Level"].ToString(),
                        row["Hash"].ToString(), row["SignedHash"].ToString(), row["Data"].ToString());
                    ++rowCount;
                }
                return transaction;


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Transaction[] transaction = new Transaction[1];
                transaction[0] = new Transaction(0, null, null, null, null, null);
                return transaction;
            }
        }

    }
}
