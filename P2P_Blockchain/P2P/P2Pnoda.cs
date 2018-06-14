using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Configuration;
using System.Net;
using System.Data.OleDb;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.Configuration;

namespace P2P_Blockchain.P2P
{
    class P2Pnoda
    {
        private OleDbConnection connection;
        private List<string> ipArr = new List<string>();
        private const bool resolveNames = true;
        private String hostIp = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
        private CountdownEvent countDown;

        public P2Pnoda(OleDbConnection connection)
        {
            this.connection = connection;
            while (true)
            {
                countDown = new CountdownEvent(1);
                //Stopwatch sw = new Stopwatch();
                //sw.Start();
                String ipBase = "192.164.12.";

                for (int i = 1; i < 255; i++)
                {
                    Ping p = new Ping();
                    String ip = ipBase + i.ToString();
                    p.PingCompleted += new PingCompletedEventHandler(P_pingCompleted);
                    countDown.AddCount();
                    p.SendAsync(ip, 200, ip);
                }

                Console.WriteLine("Найденных хостов в текущей сети {0}", ipArr.Count);
                new Thread(Server).Start();
                for (int i = 0; i < ipArr.Count; i++)
                {
                    Console.WriteLine("Ожидание подключения клиента по адресу {0}", ipArr[i]);
                    new Client(this.connection, ipArr[i]);
                }
                Thread.Sleep(5000);
                countDown.Reset();
                countDown.Dispose();
                ipArr.Clear();
            }
        }

        private void P_pingCompleted(object sender, PingCompletedEventArgs e)
        {
            String ip = (String)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                String name;
                if (resolveNames)
                {
                    try
                    {
                        name = Dns.GetHostEntry(ip).HostName;
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine(ex);
                        name = "?";
                    }
                    if (ip != hostIp)
                    {
                        Console.WriteLine("Хост {0}, адрес {1}", name, ip);
                        ipArr.Add(ip);
                    }
                }
                else
                {
                    if (ip != hostIp)
                    {
                        Console.WriteLine("Адрес {0}", ip);
                        ipArr.Add(ip);
                    }
                }
            }
            else if (e.Reply == null)
            {
                Console.WriteLine("...");
            }
        }

        private void Server(object obj)
        {
            new Server(connection);
        }
    }
}
