using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Data;
using System.Data.OleDb;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace P2P_Blockchain.P2P
{
    class Client
    {
        private IPHostEntry host;
        private IPAddress addr;
        private IPEndPoint point;
        private OleDbConnection connection;

        public Client(OleDbConnection connection, string ip)
        {
            this.connection = connection;
            host = Dns.GetHostByName(ip);
            addr = host.AddressList[0];
            point = new IPEndPoint(addr, 12000);

            Socket connect = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                connect.Connect(point);

                string hash = null;

                try
                {
                    this.connection.Open();
                    OleDbCommand command = this.connection.CreateCommand();
                    command.CommandText = "SELECT TOP 1 * FROM BlockList ORDER BY ID DESC";
                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    this.connection.Close();
                    hash = table.Rows[0]["HASH"].ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                string size = Encoding.UTF8.GetByteCount(hash).ToString();
                connect.Send(Encoding.UTF8.GetBytes(size));
                Console.WriteLine("Отправлен размер хэша {0}", size);

                byte[] answerBytes = new byte[1024];
                connect.Receive(answerBytes);
                Console.WriteLine("Получен ОК");

                byte[] hashBytes = Encoding.UTF8.GetBytes(hash);
                connect.Send(hashBytes);
                Console.WriteLine("Отправлен хэш {0}", hash);

                int answerRec = connect.Receive(answerBytes);
                string answer = Encoding.UTF8.GetString(answerBytes, 0, answerRec);
                Console.WriteLine("Получен вариант {0}", answer);

                byte[] sizeBytes = new byte[1024];
                switch (answer)
                {
                    case "1": break;

                    case "2":
                        int sizeRec = connect.Receive(sizeBytes);
                        string size2 = Encoding.UTF8.GetString(sizeBytes, 0, sizeRec);
                        Console.WriteLine("Получен размер бд {0}", size2);

                        connect.Send(Encoding.UTF8.GetBytes("OK"));
                        Console.WriteLine("Отправлен ОК");

                        byte[] tableBytes = new byte[Convert.ToInt32(size2)];

                        int tableRec = connect.Receive(tableBytes);
                        Console.WriteLine("Получена бд");
                        DataTable newTable = new DataTable();

                        using (MemoryStream stream = new MemoryStream(tableBytes))
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            newTable = (DataTable)formatter.Deserialize(stream);
                        }
                        try
                        {
                            this.connection.Open();
                            OleDbCommand command = connection.CreateCommand();
                            OleDbTransaction transaction = this.connection.BeginTransaction();
                            foreach (DataRow row in newTable.Rows)
                            {
                                command.CommandText = String.Format("INSERT INTO BlockList (PrevHash, Hash, Data) VALUES ('{0}', '{1}', '{2}')", row["PrevHash"], row["Hash"], row["Data"]);
                                
                                command.Transaction = transaction;
                                command.ExecuteNonQuery();
                                
                            }
                            Console.WriteLine("БД была успешно обновлена");
                            transaction.Commit();
                            this.connection.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;

                    case "3":
                        int hashRec = connect.Receive(hashBytes);
                        hash = Encoding.UTF8.GetString(hashBytes, 0, hashRec);
                        Console.WriteLine("Получен хэш {0}", hash);
                        DataTable table = new DataTable();
                        try
                        {
                            this.connection.Open();
                            OleDbCommand command = this.connection.CreateCommand();
                            command.CommandText = "SELECT * FROM BlockList";
                            OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                            adapter.Fill(table);
                            this.connection.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        int id = 0;
                        bool flag = false;
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            if (hash == table.Rows[i]["Hash"].ToString())
                            {
                                id = i;
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            connect.Send(Encoding.UTF8.GetBytes("OK"));
                            Console.WriteLine("Отправлено ОК");
                            break;
                        }

                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            if (i <= id) table.Rows.RemoveAt(0);
                        }
                        IFormatter formatter2 = new BinaryFormatter();
                        MemoryStream stream2 = new MemoryStream();
                        formatter2.Serialize(stream2, table);
                        tableBytes = stream2.GetBuffer();

                        connect.Send(Encoding.UTF8.GetBytes(tableBytes.Count().ToString()));
                        Console.WriteLine("Отправлен размер бд");

                        connect.Receive(answerBytes);
                        Console.WriteLine("Получено ОК");

                        connect.Send(tableBytes);
                        Console.WriteLine("Отправлена бд");
                        break;
                }
                connect.Shutdown(SocketShutdown.Both);
                connect.Close();
            }
            catch
            {
                Console.WriteLine("Обрыв соединения");
            }
        }
    }
}
