using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Data.OleDb;
using System.Threading;
using System.Text.RegularExpressions;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace P2P_Blockchain.P2P
{
    class Server
    {
        private IPHostEntry host;
        private IPAddress addr;
        private IPEndPoint point;
        private OleDbConnection connection;

        public Server(OleDbConnection connection)
        {
            this.connection = connection;
            this.host = Dns.GetHostByName(Dns.GetHostName());
            this.addr = host.AddressList[0];
            this.point = new IPEndPoint(addr, 12000);

            Socket sListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {

                sListener.Bind(point);
                sListener.Listen(200);

                while (true)
                {
                    Socket connect = sListener.Accept();

                    new Thread(ThreadEvent).Start(connect);
                }
            }
            catch
            {
                Console.WriteLine("Обрыв соединения");
            }
        }

        private void ThreadEvent(object obj)
        {
            try
            {
                Socket connect = (Socket)obj;

                byte[] sizeBytes = new byte[1024];
                int sizeRec = connect.Receive(sizeBytes);
                string size = Encoding.UTF8.GetString(sizeBytes, 0, sizeRec);
                Console.WriteLine("Получен размер хэша {0}", size);

                connect.Send(Encoding.UTF8.GetBytes("OK"));
                Console.WriteLine("Отправленно OK");

                byte[] hashBytes = new byte[Convert.ToInt32(size)];
                int hashRec = connect.Receive(hashBytes);
                string hash = Encoding.UTF8.GetString(hashBytes, 0, hashRec);
                Console.WriteLine("Получен хэш {0}", hash);

                string answer = null;

                DataTable table = new DataTable();
                DataTable table2 = new DataTable();
                int id = 0;

                try
                {
                    connection.Open();
                    OleDbCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM BlockList";
                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                    adapter.Fill(table);
                    connection.Close();
                    bool flag = false;
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        if (table.Rows[i]["Hash"].ToString() == hash && i == table.Rows.Count - 1)
                        {
                            answer = Convert.ToString(1);
                            flag = true;
                            break;
                        }
                        else if (table.Rows[i]["Hash"].ToString() == hash && i != table.Rows.Count - 1)
                        {
                            answer = Convert.ToString(2);
                            flag = true;
                            id = i;
                            break;
                        }
                    }
                    if (!flag) answer = Convert.ToString(3);
                    table2 = table;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                connect.Send(Encoding.UTF8.GetBytes(answer));
                Console.WriteLine("Отправленн вариант {0}", answer);
                byte[] answerBytes = new byte[1024];

                switch (answer)
                {
                    case "1": break;

                    case "2":

                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            if (i <= id) table.Rows.RemoveAt(0);
                        }
                        MemoryStream stream = new MemoryStream();
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, table);
                        byte[] tableBytes = stream.GetBuffer();
                        connect.Send(Encoding.UTF8.GetBytes(tableBytes.Count().ToString()));
                        Console.WriteLine("Отправленн размер бд");

                        connect.Receive(answerBytes);
                        Console.WriteLine("Получен ответ OK");

                        connect.Send(tableBytes);
                        Console.WriteLine("Отправленна бд");

                        break;

                    case "3":
                        hash = table2.Rows[table2.Rows.Count - 1]["Hash"].ToString();
                        hashBytes = Encoding.UTF8.GetBytes(hash);
                        connect.Send(hashBytes);
                        Console.WriteLine("Отправлен хэш {0}", hash);

                        int answerRec = connect.Receive(answerBytes);
                        Console.WriteLine("Получено ответ");
                        answer = Encoding.UTF8.GetString(answerBytes, 0, answerRec);
                        if (answer == "OK")
                        {
                            Console.WriteLine("Ответ ОК");
                            break;
                        }
                        Console.WriteLine("Ответ не ОК");
                        sizeRec = connect.Receive(sizeBytes);
                        size = Encoding.UTF8.GetString(sizeBytes, 0, sizeRec);
                        Console.WriteLine("Получен размер бд {0}", size);

                        connect.Send(Encoding.UTF8.GetBytes("OK"));
                        Console.WriteLine("ОТправлено ОК");

                        byte[] newTableBytes = new byte[Convert.ToInt32(size)];
                        int tableRec = connect.Receive(newTableBytes);
                        Console.WriteLine("Получена бд");
                        DataTable newTable2 = new DataTable();

                        using (MemoryStream stream2 = new MemoryStream(newTableBytes))
                        {
                            BinaryFormatter formatter2 = new BinaryFormatter();
                            newTable2 = (DataTable)formatter2.Deserialize(stream2);
                        }
                        try
                        {
                            connection.Open();
                            OleDbCommand command = connection.CreateCommand();
                            OleDbTransaction transaction = connection.BeginTransaction();
                            foreach (DataRow row in newTable2.Rows)
                            {
                                command.CommandText = String.Format("INSERT INTO BlockList (PrevHash, Hash, Data) VALUES ('{0}', '{1}', '{2}')", row["PrevHash"], row["Hash"], row["Data"]);

                                command.Transaction = transaction;
                                command.ExecuteNonQuery();

                            }
                            transaction.Commit();
                            connection.Close();
                            Console.WriteLine("БД была успешно изменена");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

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
