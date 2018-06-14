using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using System.Text.RegularExpressions;

namespace P2P_Blockchain.Blockchain
{
    class Block
    {
        public DataTable SelectToConnect(OleDbConnection connection, string SQLCMD)
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

        public void EditToConnect(OleDbConnection connection, string SQLCMD)
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

        public Block() { }

        public bool MakeBlock(OleDbConnection connection, string HashId)
        {
            //ВЫбор ззаголовка блока из PoolBlock по хэшу
            string SQLCMD = String.Format("SELECT * FROM PoolBlock WHERE [Hash] = '{0}'", HashId);
            DataTable TablePoolBlock = SelectToConnect(connection, SQLCMD);

            PoolBlock block = new PoolBlock(Convert.ToInt32(TablePoolBlock.Rows[0]["ID"]), TablePoolBlock.Rows[0]["Hash"].ToString(),
                TablePoolBlock.Rows[0]["HashList"].ToString(), TablePoolBlock.Rows[0]["Data"].ToString());

            string HASHList = block.HashList;
            string HASH = block.Hash;

            //Плучние ПревХэш
            SQLCMD = "SELECT TOP 1 * FROM BlockList ORDER BY ID DESC";

            DataTable table = SelectToConnect(connection, SQLCMD);

            string PrevHASH = "";

            if (table.Rows.Count == 1)
            {
                PrevHASH = table.Rows[table.Rows.Count - 1]["Hash"].ToString();
            }

            //Запись заголовка блока в BlockList
            SQLCMD = String.Format("INSERT INTO BlockList (PrevHash, Hash, Data) VALUES ('{0}', '{1}', '{2}')", PrevHASH, HASH, block.Data);
            EditToConnect(connection, SQLCMD);

            //Удаление заголовка блока из PoolBlock
            SQLCMD = String.Format("DELETE * FROM PoolBlock WHERE [Hash] = '{0}'", HASH);
            EditToConnect(connection, SQLCMD);

            return true;
        }
    }
}
