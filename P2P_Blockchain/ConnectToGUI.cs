using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data.OleDb;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Text.RegularExpressions;
using P2P_Blockchain.Blockchain;

namespace P2P_Blockchain
{
    class ConnectToGUI
    {
        private IPHostEntry iPHostEntry;
        private IPAddress address;
        private IPEndPoint iPEndPoint;
        private OleDbConnection connection;
        private String [] keys_array;
        private String username = Dns.GetHostName();
        private String privateKey, publicKey;

        public ConnectToGUI(OleDbConnection connection)
        {
            this.connection = connection;
            this.iPHostEntry = Dns.GetHostEntry("localhost");
            this.address = this.iPHostEntry.AddressList[0];
            this.iPEndPoint = new IPEndPoint(this.address, 11000);

            Socket sListener = new Socket(this.address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sListener.Bind(this.iPEndPoint);
                sListener.Listen(1);

                while (true)
                {
                    Socket handler = sListener.Accept();
                    new Thread(this.ThreadProc).Start(handler);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }

        private void ThreadProc(Object obj)
        {
            Socket handler = (Socket)obj;
            while (true)
            {
                string data = null;
                int bytesRec = 0;
                byte[] bytes = new byte[1024];
                try
                {
                    bytesRec = handler.Receive(bytes);
                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    string[] data_array = Regex.Split(data, ":");
                    switch (data_array[0])
                    {
                        case "db_select":
                            try
                            {
                                this.connection.Open();
                                OleDbCommand command = this.connection.CreateCommand();
                                command.CommandText = String.Format("SELECT * FROM {0}", data_array[2]);

                                OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                                DataTable table = new DataTable();
                                adapter.Fill(table);

                                this.connection.Close();

                                MemoryStream stream = new MemoryStream();
                                IFormatter formatter = new BinaryFormatter();
                                formatter.Serialize(stream, table);
                                byte[] msg = stream.GetBuffer();
                                handler.Send(msg);
                                Console.Write("\nБаза данных успешно отправленна на клиент");
                            }
                            catch
                            {
                                Console.Write("\nОшибка подключение к базе данных");
                            }
                            break;

                        case "keys":
                            try
                            {
                                handler.Send(Encoding.UTF8.GetBytes("ok"));
                                byte[] key_bytes = new byte[Convert.ToInt32(data_array[1])];
                                bytesRec = handler.Receive(key_bytes);
                                string answer = null;
                                answer += Encoding.UTF8.GetString(key_bytes, 0, bytesRec);
                                keys_array = Regex.Split(answer, @"\---([^\---\---]+)\---");

                                publicKey = keys_array[2];
                                privateKey = keys_array[4];

                                Users user = new Users(RSA.GetMd5Hash(publicKey), publicKey, "admin", "vlad", "kubits", "sergeyevith", "6554", "64562485628756234");
                                String Data = user.ConvertNewUserToData();
                                Transaction transaction = new Transaction();
                                transaction.SetTransaction(this.connection, privateKey, publicKey, "admin", Data);

                                try
                                {
                                    connection.Open();

                                    OleDbCommand command = connection.CreateCommand();
                                    command.CommandText = "SELECT * FROM Users";
                                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                                    DataTable table = new DataTable();
                                    adapter.Fill(table);

                                    connection.Close();

                                    string login = RSA.GetMd5Hash(keys_array[2]);
                                    string level = null;
                                    foreach(DataRow row in table.Rows)
                                    {
                                        if (login == row["Login"].ToString()) { level = row["Level"].ToString(); break; }
                                    }
                                    handler.Send(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", login, level)));

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                                
                                handler.Send(Encoding.UTF8.GetBytes("ok"));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                            break;
                        default: break;
                    }
                }
                catch
                {
                    Console.WriteLine("Сервер завершил соединение с клиентом");
                    Console.Write("Ожидаем соединение через порт {0}", iPEndPoint);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    break;
                }
            }


        }
    }
}
