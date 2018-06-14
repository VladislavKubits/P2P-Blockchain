using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace GUI
{
    class ConnectToConsole
    {
        public Socket sender;
        public byte[] bytes;

        public ConnectToConsole()
        {
            int port = 11000;

            // Буфер для входящих данных
            bytes = new byte[1024];

            // Устанавливаем порт работы для сокета
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

            sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Соединяем сокет с удаленной точкой
            sender.Connect(ipEndPoint);
        }

        public string SendMessage(string message)
        {
            // клас отправки сообщения на консоль и получения ответа от него
            byte[] msg = Encoding.UTF8.GetBytes(message);
            int len = msg.Length; //
           
            int bytesSent = sender.Send(msg);
            int bytesRec = sender.Receive(bytes);
            return Encoding.UTF8.GetString(bytes, 0, bytesRec);             
        }

        ~ConnectToConsole()
        {
            // Закрытие сокитов
            if (sender != null)
            {
                try
                {
                    sender.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex) // если sender был пуст и случилось исключение то просто закрываем сокет
                {
                    sender.Close();
                    return;
                }

                sender.Close();
            }


        }

    }
}
