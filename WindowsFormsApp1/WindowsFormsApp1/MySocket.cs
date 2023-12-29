using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace WindowsFormsApp1
{
    internal class MySocket
    {
        protected List<StateObject> activeConnections { get; set; } = new List<StateObject>();
        protected IPAddress ipAddress;
        protected IPEndPoint remoteEP;
        protected Socket socket;
        protected byte[] buf = new byte[1024];

        public MySocket(IPAddress iaddr, int portno)
        {
            // IPアドレスやポートの設定
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHostInfo.AddressList[0];
            remoteEP = new IPEndPoint(ipAddress, portno);
        }

        public void Listen()
        {
            // ソケットを作成
            Socket listner = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listner.Bind(remoteEP);
            listner.Listen(0);
            _ = listner.BeginAccept(new AsyncCallback(AcceptCallback), listner);
            
        }

        public void Connect()
        {
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEP);

            string msg = "test";
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            socket.Send(data);

        }

        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket soc = listener.EndAccept(ar);
                StateObject state = new StateObject
                {
                    workSocket = soc
                };
                soc.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                activeConnections.Add(state);

                // 次のアクセプト待機
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (System.ObjectDisposedException)
            {
                System.Console.WriteLine("Connection closed.");
                return;
            }

        }
        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket soc = state.workSocket;
            try
            {
                int bytesRead = soc.EndReceive(ar);
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    content = state.sb.ToString();
                }
            }
            catch (Exception )
            {
                activeConnections.Remove(state);
            }

        }

        private void Send(Socket handler, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }



        protected class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
            public bool sendDataFlag = false;
        }

    }
}
