using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace serveur
{
    class Program
    {
        static byte [] buffer {get; set;}
        static Socket sck;

        static void Main(string[] args)
        {
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.Bind(new IPEndPoint(0,1234));
            sck.Listen(1000);
            Socket client1 =sck.Accept();
            Socket client2 = sck.Accept();
        }
    }
}
