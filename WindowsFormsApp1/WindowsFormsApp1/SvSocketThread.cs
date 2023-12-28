using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    internal class SvSocketThread
    {
        // スレッド待機用
        private ManualResetEvent AllDone = new ManualResetEvent(false);
        private Task taskListen;

        // サーバーのエンドポイント
        public IPEndPoint IPEndPoint { get; }

        // 接続中のクライアント(スレッドセーフコレクション)
        public SynchronizedCollection<Socket> ClientSockets { get; } = new SynchronizedCollection<Socket>();



        //送受信文字列エンコード
        private Encoding enc = Encoding.UTF8;

        /** イベント **/
        //データ受信イベント
        public delegate void ReceiveEventHandler(object sender, string e);
        public event ReceiveEventHandler OnSvReceiveData;

        //データ送信イベント
        public delegate void SendEventHandler(object sender, string e);
        public event SendEventHandler OnSvSendData;

        //接続断イベント
        public delegate void DisconnectedEventHandler(object sender, EventArgs e, string s);
        public event DisconnectedEventHandler OnSvDisconnected;

        //接続OKイベント
        public delegate void ConnectedEventHandler(EventArgs e, string s);
        public event ConnectedEventHandler OnSvConnected;


        // コンストラクタ
        public SvSocketThread(int port)
        {
            this.IPEndPoint = new IPEndPoint(IPAddress.Any, port);
        }

        // サーバー処理開始
        public void ServiceStart()
        {
            taskListen = Task.Factory.StartNew(() =>
            {
                Run();
            });
        }

        // サーバー処理終了
        public void ServiceEnd()
        {
            foreach (var clientSocket in this.ClientSockets) clientSocket?.Close();
        }

        // サーバー起動
        public void Run()
        {
            using (var listenerSocket = new Socket(AddressFamily.InterNetwork,
                                            SocketType.Stream, ProtocolType.Tcp))
            {
                // ソケットをアドレスにバインドする
                listenerSocket.SetSocketOption(SocketOptionLevel.Socket,
                                              SocketOptionName.ReuseAddress, true);
                listenerSocket.Bind(this.IPEndPoint);

                // 接続待機開始
                listenerSocket.Listen(10);

                // 接続待機のループ
                while (true)
                {
                    AllDone.Reset();
                    listenerSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenerSocket);
                    // 接続があるまでスレッドを待機させる
                    AllDone.WaitOne();
                }
            }
        }

        // サーバー終了処理
        public void Close()
        {
            foreach (var clientSocket in this.ClientSockets) clientSocket?.Close();
        }

        // 接続受付時のコールバック処理
        private void AcceptCallback(IAsyncResult asyncResult)
        {
            // 待機スレッドが進行するようにシグナルをセット
            AllDone.Set();

            // ソケットを取得
            var listenerSocket = asyncResult.AsyncState as Socket;
            var clientSocket = listenerSocket.EndAccept(asyncResult);

            // 接続中のクライアントを追加
            ClientSockets.Add(clientSocket);
            Console.WriteLine($"接続: {clientSocket.RemoteEndPoint}");

            //接続OKイベント発生
            OnSvConnected(new EventArgs(), $"接続 : {clientSocket.RemoteEndPoint}");

            // StateObjectを作成
            var state = new StateObject();
            state.ClientSocket = clientSocket;

            // 受信時のコードバック処理を設定
            clientSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize,
                                        0, new AsyncCallback(ReceiveCallback), state);
        }

        // 受信時のコードバック処理
        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            // StateObjectとクライアントソケットを取得
            var state = asyncResult.AsyncState as StateObject;
            var clientSocket = state.ClientSocket;

            try
            {
                // クライアントソケットから受信データを取得終了
                int bytes = clientSocket.EndReceive(asyncResult);

                if (bytes > 0)
                {
                    // 受信した文字列を表示
                    var content = enc.GetString(state.Buffer, 0, bytes);

                    //データ受信イベント発生
                    OnSvReceiveData(this, content);

                    // 受信文字列を接続中全クライアントに送信。
                    SendAllClient(content);

                    // 受信時のコードバック処理を再設定
                    clientSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize,
                                              0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // 0バイトデータの受信時は、切断されたとき
                    clientSocket.Close();
                    this.ClientSockets.Remove(clientSocket);

                    //接続断イベント発生
                    string msg = "0バイトデータの受信";
                    OnSvDisconnected(this, new EventArgs(), msg);
                }
            }
            catch (SocketException e)
            {
                if (e.NativeErrorCode.Equals(10054))
                {
                    // 既存の接続が、リモート ホストによって強制的に切断されました
                    // 保持しているクライアントの情報をクリアする
                    clientSocket.Close();
                    this.ClientSockets.Remove(clientSocket);

                    //接続断イベント発生
                    OnSvDisconnected(this, new EventArgs(), e.Message);
                }
                else
                {
                    string msg = string.Format("Disconnected!: error code {0} : {1}",
                                                            e.NativeErrorCode, e.Message);
                    Console.Write(msg);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                Console.Write(msg);
            }
        }

        // クライアントへのメッセージ送信処理
        private void Send(Socket clientSocket, String data)
        {
            try
            {
                // 文字列に変換し送信
                var bytes = enc.GetBytes(data);

                //データ送信イベント発生
                OnSvSendData(this, data);

                clientSocket.BeginSend(bytes, 0, bytes.Length,
                              0, new AsyncCallback(SendCallback), clientSocket);
            }
            catch (SocketException e)
            {
                if (e.NativeErrorCode.Equals(10054))
                {
                    // 既存の接続が、リモート ホストによって強制的に切断されました
                    //接続断イベント発生
                    OnSvDisconnected(this, new EventArgs(), e.Message);
                }
                else
                {
                    string msg = string.Format("Disconnected!: error code {0} : {1}",
                                                     e.NativeErrorCode, e.Message);
                    Console.Write(msg);
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("Socket exception: {0}", ex.Message);
                Console.Write(msg);
            }
        }

        // 送信時のコールバック処理
        private static void SendCallback(IAsyncResult asyncResult)
        {
            try
            {
                // クライアントソケットへのデータ送信処理を完了する
                var clientSocket = asyncResult.AsyncState as Socket;
                var byteSize = clientSocket.EndSend(asyncResult);
                Console.WriteLine($"送信結果: {byteSize}バイト [{clientSocket.RemoteEndPoint}]");
            }
            catch (Exception e)
            {
                string msg = e.Message;
                Console.Write(msg);
            }
        }

        // 全クライアントへの送信処理
        public void SendAllClient(string data)
        {
            foreach (var clientSocket in this.ClientSockets)
            {
                Send(clientSocket, data);
            }
        }
    }

    // 接続されたクライアントの情報を格納するクラス
    public class StateObject
    {
        public Socket ClientSocket { get; set; }
        public const int BufferSize = 1024;
        public byte[] Buffer { get; } = new byte[BufferSize];
    }
}
