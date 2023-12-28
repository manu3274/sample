using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    internal class MySocket
    {
        protected IPAddress ipAddress;
        protected IPEndPoint remoteEP;
        protected Socket socket;

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
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


        }
    }
}
