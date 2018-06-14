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
using P2P_Blockchain.P2P;
using P2P_Blockchain.Blockchain;
using System.Windows.Forms;


namespace P2P_Blockchain
{
    class Program
    {
        private static OleDbConnection connection = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Path.Combine(Application.StartupPath, "Blockchain.mdb"));
        public static string privateKey = null, publicKey = null; 

        static void Main(string [] args)
        {
            //new Thread(P2PnodaStart).Start();
            
            new Thread(ConnectToGUI).Start();
            

        }
        private static void P2PnodaStart(object obj)
        {
            P2Pnoda noda = new P2Pnoda(connection);
        }

        private static void ConnectToGUI(object obj)
        {
            ConnectToGUI connectToGUI = new ConnectToGUI(connection);
        }
    }
}
