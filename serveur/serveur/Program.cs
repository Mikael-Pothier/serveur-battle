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
        static byte[] buffer { get; set; }
        static Socket sck;
        static Socket client1=null;
        static Socket client2=null;

        static void Main(string[] args)
        {

            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.Bind(new IPEndPoint(0, 1234));
            sck.Listen(1000);

            Console.WriteLine("En attente de connexion");

            while(true)
            {
                if(client1 == null)
                {
                   client1 = sck.Accept();
                   Console.WriteLine("Joueur1 connecté");
                }
                if(client2 == null)
                {
                   client2 = sck.Accept();
                   Console.WriteLine("Joueur2 connecté");
                }

                if(!SocketConnected(client1))
                {
                    client1 = null;
                }
                else if(!SocketConnected(client2))
                {
                    client2 = null;
                }
            }
            ReceiveData(client1);
            ReceiveData(client2);

            
            client1.Close();
            client2.Close();
            sck.Close();
        }

        private static void ReceiveData(Socket client)
        {
            try
            {
                buffer = new byte[client.SendBufferSize];
                int bytesRead = client.Receive(buffer);
                byte[] formatted = new byte[bytesRead];
                for (int i = 0; i < bytesRead; i++)
                {
                    formatted[i] = buffer[i];
                }
                string strData = Encoding.ASCII.GetString(formatted);
                Console.Write(strData + "\r\n");
            }
            catch { Console.Write("Erreur de telechargement des données"); }
        }

        static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }
    }
}
