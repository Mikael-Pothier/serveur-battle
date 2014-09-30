using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using BateauDLL;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace serveur
{
    class Serveur
    {
        const int touche = 1;
        const int manque = 2;
        const int bateau = 3;
        static byte[] buffer { get; set; }
        static Socket sck;
        static Joueur player1;
        static Joueur player2;
        static Socket client1=null;
        static Socket client2=null;
        static int[,] matriceAttaqueJ1 = new int[10, 10];
        static int[,] matriceAttaqueJ2 = new int[10, 10];

        static void Main(string[] args)
        {
            player1 = new Joueur("player1");
            player2 = new Joueur("player2");
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

                if (SocketConnected(client1) && SocketConnected(client2))
                {
                    
                    player1=ReceiveDataBateau(client1);
                    player2=ReceiveDataBateau(client2);
                    setMatrice(player1, matriceAttaqueJ1);
                    setMatrice(player2, matriceAttaqueJ2);

                    position PositionAttaque = new position();
                    do
                    {
                        client2.Blocking = true;
                        ReceiveData(client1,PositionAttaque);
                        if (estToucher(PositionAttaque,matriceAttaqueJ2))
                        {
                            PositionAttaque = null;
                            //envoyer message client
                        }
                    } while (PositionAttaque == null) ;
                    int resultat = getResultat(PositionAttaque, matriceAttaqueJ2);
                    setResultat(PositionAttaque,matriceAttaqueJ2,resultat);
                }
            }
            client1.Close();
            client2.Close();
            sck.Close();
        }
        private static void setResultat(position pos, int[,] matrice,int resultat)
        {
            matrice[pos.x, pos.y] = resultat;
        }
        //retourne le resultat(toucher ou manquer)
        private static int getResultat(position pos, int[,] matrice)
        {
            int res = 0;
            if (matrice[pos.x, pos.y] == bateau)
                    res = touche;
                else
                    res = manque;
            return res;
        }
        private static bool estToucher(position pos,int [,] matrice)
        {
                if (matriceAttaqueJ2[pos.x, pos.y] == touche || matriceAttaqueJ2[pos.x, pos.y] == manque) 
                {
                    return false;
                }
            return true;
        }
        private static void setMatrice(Joueur player, int[,] matrice) 
        {
            for(int i=0;i<player.PorteAvion.longueur;++i)
                matrice[player.PorteAvion.cases[i].x, player.PorteAvion.cases[i].y] = bateau;

            for(int i=0;i<player.ContreTorpille.longueur;++i)
                matrice[player.ContreTorpille.cases[i].x, player.ContreTorpille.cases[i].y] = bateau;
            for(int i=0;i<player.Croiseur.longueur;++i)
                matrice[player.Croiseur.cases[i].x,player.Croiseur.cases[i].y]=bateau;
            for(int i=0;i<player.SousMarin.longueur;++i)
                matrice[player.SousMarin.cases[i].x,player.SousMarin.cases[i].y]=bateau;
            for(int i=0;i<player.Torpilleur.longueur;++i)
                matrice[player.Torpilleur.cases[i].x,player.Torpilleur.cases[i].y]=bateau;
        }
        private static Joueur ReceiveDataBateau(Socket client)
        {
            Joueur joueur = null;
            try
            {
                buffer = new byte[client.SendBufferSize];
                int bytesRead = client.Receive(buffer);
                byte[] formatted = new byte[bytesRead];
                BinaryFormatter receive = new BinaryFormatter();

                for (int i = 0; i < bytesRead; i++)
                {
                    formatted[i] = buffer[i];
                }
                using (var recstream = new MemoryStream(formatted))
                {
                    joueur = receive.Deserialize(recstream) as Joueur;
                }

            }
            catch {Console.Write("Erreur de telechargement des données");}
            return joueur;
        }

        private static void ReceiveData(Socket client,position pos)
        {
            buffer = new byte[client.SendBufferSize];
            int bytesRead = client.Receive(buffer);
            byte[] formatted = new byte[bytesRead];
            BinaryFormatter receive = new BinaryFormatter();
            for (int i = 0; i < bytesRead; i++)
            {
                formatted[i] = buffer[i];
            }
            using (var recstream = new MemoryStream(formatted))
            {
                pos = receive.Deserialize(recstream) as position;
            }
        }

        //static private void setBateauJoueur(string[] data, Joueur player) { 
        //    if(data[0] == player.PorteAvion.nom){
        //        player.PorteAvion.debut.x = int.Parse(data[1]);
        //        player.PorteAvion.debut.y = int.Parse(data[2]);
        //        player.PorteAvion.fin.x = int.Parse(data[3]);
        //        player.PorteAvion.fin.y = int.Parse(data[4]);
        //    }
        //    else if(data[0] == player.Croiseur.nom){
        //        player.Croiseur.debut.x = int.Parse(data[1]);
        //        player.Croiseur.debut.y = int.Parse(data[2]);
        //        player.Croiseur.fin.x = int.Parse(data[3]);
        //        player.Croiseur.fin.y = int.Parse(data[4]);            
        //    }
        //    else if(data[0] == player.ContreTorpille.nom){
        //        player.ContreTorpille.debut.x = int.Parse(data[1]);
        //        player.ContreTorpille.debut.y = int.Parse(data[2]);
        //        player.ContreTorpille.fin.x = int.Parse(data[3]);
        //        player.ContreTorpille.fin.y = int.Parse(data[4]);          
        //    }
        //    else if (data[0] == player.SousMarin.nom)
        //    {
        //        player.SousMarin.debut.x = int.Parse(data[1]);
        //        player.SousMarin.debut.y = int.Parse(data[2]);
        //        player.SousMarin.fin.x = int.Parse(data[3]);
        //        player.SousMarin.fin.y = int.Parse(data[4]);
        //    }
        //    else {
        //        player.Torpilleur.debut.x = int.Parse(data[1]);
        //        player.Torpilleur.debut.y = int.Parse(data[2]);
        //        player.Torpilleur.fin.x = int.Parse(data[3]);
        //        player.Torpilleur.fin.y = int.Parse(data[4]);            
        //    } 
        //}

        static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if ((part1 && part2) || !s.Connected)
                return false;
            else
                return true;
        }

        private static void MatriceJoueur(Joueur player)
        {
            if (player.Nom_ == "player1")
            {

                matriceAttaqueJ1[0,0] = 1;
            }
            else
            {

            }
        }
    }
}
