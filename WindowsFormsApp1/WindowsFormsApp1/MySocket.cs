using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace WindowsFormsApp1
{
    internal class MySocket
    {
        protected IPAddress ipAddress;
        protected IPEndPoint remoteEP;
        protected Socket socket;
        protected byte[] buf = new byte[1024];

        public MySocket()
        {
            // IPアドレスやポートの設定
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHostInfo.AddressList[0];
            remoteEP = new IPEndPoint(ipAddress, 11000);
        }

        public void Listen()
        {
            // ソケットを作成
            Socket istener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            istener.Bind(remoteEP);
            istener.Listen(0);
            socket = istener.Accept();

            socket.Receive(buf);
            Console.WriteLine(buf);

        }

        public void Connect()
        {
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEP);

            string msg = "test";
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            socket.Send(data);

        }
    }
}
