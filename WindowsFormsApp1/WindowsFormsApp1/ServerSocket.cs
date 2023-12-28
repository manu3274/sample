using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    internal class ServerSocket: MySocket
    {
        public void Listen()
        {
            socket.Bind(remoteEP);
            socket.Listen(0);
        }
    }
}
