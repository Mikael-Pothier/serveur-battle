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
using System.Runtime.Serialization;

namespace serveur
{
    class Serveur
    {
        //pour meetre dans la matrice
        const int touche = 1;
        const int manque = 2;
        const int bateau = 3;
        static byte[] buffer { get; set; }
        //socket serveur
        static Socket sck;
        //jouer (dll) il contient les bateaux ainsi qu'un nom
        static Joueur player1;
        static Joueur player2;
        //les socket des 2 clients. le socket 1 va seulement communiquer avec le client 1 et le socket 2 va seulement communiquer avec le client 2. les deux socket peut envoyer et recevoir
        static Socket client1=null;
        static Socket client2=null;
        //matrice pour stocke les coups et la position des bateaux
        static int[,] matriceAttaqueJ1 = new int[10, 10];
        static int[,] matriceAttaqueJ2 = new int[10, 10];

        static void Main(string[] args)
        {
            //---------------------INITIALISATION VARIABLE--------------------------------------------------------------------------------
            player1 = new Joueur("player1");
            player2 = new Joueur("player2");
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.Bind(new IPEndPoint(0, 1234));
            sck.Listen(1000);
            
            Console.WriteLine("En attente de connexion");

            while(true)
            {
                //if car si il se deconnecte avant de partir la partis il ne va pas aprtier la partis et revenir ici, donc le if permet de savoir quel client a été deconnect
                if(client1 == null)
                {
                   client1 = sck.Accept();
                   Console.WriteLine("Joueur1 connecté");
                }
                if(client2 ==null)
                {
                   client2 = sck.Accept();
                   Console.WriteLine("Joueur2 connecté");
                }
                if (client1 != null && client2 !=null)
                {
                    //envoye au 2 clients que la parti est commencée
                    sendClient(client1, "La partie est commencee");
                    sendClient(client2, "La partie est commencee");
                    //recois la position des bateau(il envoye un joueur)
                    player1=ReceiveDataBateau(client1);
                    player2=ReceiveDataBateau(client2);
                    //envoye au client qui est le joueur 1 et qui est le client 2(le client 1 est toujours celui qui click en premier sur pret)
                    sendClient(client1, "1");
                    sendClient(client2, "2");
                    //set la matrice du joueur pour savoir la position des bateaux
                    setMatrice(player1, matriceAttaqueJ1);
                    setMatrice(player2, matriceAttaqueJ2);
                    //tant que personne a perdu ou que personne est partis
                    while (!aperdu(player1) && !aperdu(player2) && client1!= null && client2!= null)
                    {
                        jouer();
                    }
                    //savoir quel joueur a perdu
                    if (aperdu(player1))
                    {
                        sendClient(client1, "vous avez perdus");
                        sendClient(client2, "vous avez gagnes");
                        lire(client1);
                        lire(client2);
                    }
                    else if(aperdu(player2))
                    {
                        sendClient(client2, "vous avez perdus");
                        sendClient(client1, "vous avez gagnes");
                        lire(client1);
                        lire(client2);                       
                    }
                    resetPartie();

                }
            }
        }
        //resetPartie recommence la partie. il met les clients a null et rement les matrices des joueurs a 0
        static private void resetPartie() 
        { 
            for (int i=0;i<10;++i)
            {
                for (int j = 0; j < 10;++j )
                {
                    matriceAttaqueJ1[i,j] = 0;
                    matriceAttaqueJ2[i,j] = 0;
                }
            }
            client1 = null;
            client2 = null;
        }
        //la place ou le client 1 attaque et recois les messages selon les coups qu'il a fait
        private static void jouer()
        {
            String text = attaquer(client1, client2, player2, matriceAttaqueJ2);
            //si le joueur 2 est mort alors il ne fera pas de coup ou si il un cleint s'en va(impossible que le client 1 perde car il fait l'attaque)
            if (!aperdu(player2) && client1 != null && client2 != null)
            {
                //envoye le message au client qui a attaque
                sendClient(client1, text);
                //envoye le message au client qui ce fait attaquer
                sendClient(client2, text);
                text = attaquer(client2, client1, player1, matriceAttaqueJ1);
            }
            //meme verification sauf  avec le client 1
            if (!aperdu(player1) && client1!= null && client2!= null)
            {
                sendClient(client1, text);
                sendClient(client2, text);
            }        
        }
        //attaquer() prend les 2 clients, le joueur qui se fait attaquer et la matrice du joueur qui se fait attaquer.
        //il va recevoir la position d'attaque du client pour ensuite aller verifier dans la matrice du joueur qui se fait attaquer si il y a un bateau ou non a la position d'attaque
        //il retourne le message selon ce qui c'est passer
        private static String attaquer(Socket clientAttaque, Socket clientWait,Joueur playerAttaquer, int[,] matrice)
        {
            position PositionAttaque = new position();
            //si il n'y a personne qui a quitter ou que la position d'attaque est null on attend la reponse du client
            do
            {
                //clientWait.Blocking = true; inutile car le client est geler et ne peut donc rien faire
                PositionAttaque = ReceiveData(clientAttaque,clientWait);
            } while (PositionAttaque == null && client1!= null && client2 != null);
            //peut etre null si le client a quitter
            if (PositionAttaque != null)
            {
                //recois le message si toucher ou manquer
                int resultat = getResultat(PositionAttaque, matrice);
                //le met dans la matrice
                setResultat(PositionAttaque, matrice, resultat);
                String message = "";
                if (resultat == touche)
                {
                    //verifie si c'est un bateau coule ou seulement touche
                    message = BateauTouche(PositionAttaque, playerAttaquer);
                }
                else
                {
                    message = "la cible a ete manquer";
                }
                //renvoye le resultat de la reponse, la position et le message (il envoye le resultat et la position pour que le client puisse savoir de quel couleur placer la case qu'il a attaquer)
                String text = resultat + "," + PositionAttaque.x.ToString() + "," + PositionAttaque.y.ToString() + "," + message;
                return text;
            }
            return "";
        }
        //bateauTouche() prend la position d'attaque et le joueur qui se fait attaquer
        //il dit au bateau qu'il a été touché a cette position et vérifie si le bateau est coulé
        //retourne le message soit le bateau est touché ou coulé
        private static String BateauTouche(position pos,Joueur player)
        {
            bool trouvé = false;
            String message = "Bateau touche";
            //porte-avion
            for (int i = 0;!porteAvionCoule(player) && i < player.PorteAvion.longueur && !trouvé; ++i)
            {
                if (player.PorteAvion.cases[i].x == pos.x && player.PorteAvion.cases[i].y == pos.y)
                {
                    player.PorteAvion.estTouche[i] = true;
                    if (porteAvionCoule(player))
                        message="Porte avion coule!";
                    trouvé = true;
                }
            }
            //contre torpille
            for (int i = 0; !ContreTorpilleCoule(player) && i < player.ContreTorpille.longueur && !trouvé; ++i)
            {
                if (player.ContreTorpille.cases[i].x == pos.x && player.ContreTorpille.cases[i].y == pos.y)
                {
                    player.ContreTorpille.estTouche[i] = true;
                    if(ContreTorpilleCoule(player))
                        message="ContreTorpille coule";
                    trouvé = true;
                }
            }
            //croisseur
            for (int i = 0; !CroiseurCoule(player) && i < player.Croiseur.longueur && !trouvé; ++i)
            {
                if (player.Croiseur.cases[i].x == pos.x && player.Croiseur.cases[i].y == pos.y)
                {
                    player.Croiseur.estTouche[i] = true;
                    if(CroiseurCoule(player))
                        message= "Croiseur coule";
                    trouvé = true;
                }
            }
            //sousMarin
            for (int i = 0; !SousMarinCoule(player) && i < player.SousMarin.longueur && !trouvé; ++i)
            {
                if (player.SousMarin.cases[i].x == pos.x && player.SousMarin.cases[i].y == pos.y)
                {
                    player.SousMarin.estTouche[i] = true;
                    if (SousMarinCoule(player))
                        message = "Sous-marin coule";
                    trouvé = true;
                }
            }
            //torpilleur
            for (int i = 0; !TorpilleurCoule(player) && i < player.Torpilleur.longueur && !trouvé; ++i)
            {
                if (player.Torpilleur.cases[i].x == pos.x && player.Torpilleur.cases[i].y == pos.y)
                {
                    player.Torpilleur.estTouche[i] = true;
                    if (TorpilleurCoule(player))
                        message = "Torpilleur coule";
                    trouvé = true;
                }
            }
            return message;
        }
        //envoye une string au client choisis
        private static void sendClient(Socket client,String text) 
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(text);
                client.Send(data);
            }
            catch { Console.Write("Erreur de telechargement des donnees"); }
        }
        //setResultat() prend une position,une matrice et un resultat
        // il set le resultat du coup dans la matrice a la position voulu
        private static void setResultat(position pos, int[,] matrice,int resultat)
        {
            matrice[pos.x, pos.y] = resultat;
        }
        //getresultat() prend une position et une matrice
        //verifie si c'est un bateau ou non
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
        //setMatrice() prend un joueur et une matrice
        //place les bateaux dans la matrice du joueur
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
        //ReceiveDataBateau() prend un socket
        //lis les donnés du client pour prendre un client et la position des bateaux
        //retourne un joueur
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
        //ReceiveDataBateau() prend un Socket
        //lis des données d'un client pour prendre la position d'attaque
        //retourne une position
        private static position ReceiveData(Socket client,Socket clientWait)
        {
            position pos = null;
            buffer = new byte[client.SendBufferSize];
            int bytesRead = client.Receive(buffer);
            byte[] formatted = new byte[bytesRead];
            BinaryFormatter receive = new BinaryFormatter();
            try
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    formatted[i] = buffer[i];
                }

                using (var recstream = new MemoryStream(formatted))
                {
                    pos = receive.Deserialize(recstream) as position;
                }
            }
            //si la position n'est pas une position, le client c'est déconnecter et il envoye a l'autre client que son adversaire est parti
            catch(SerializationException serial)
            {
                client1 = null;
                sendClient(clientWait,"L'adversaire est parti");
                client2 = null;
            }
            return pos;
        }
        //lire() prend un socket
        // lis un message ruce du client voulu
        //retourne un string
        private static String lire(Socket client)
        {
            String message = null;
            do
            {
                message = recevoirResultat(client);
            } while (message == null);
            return message;
        }
        //recevoirResultat()prend un socket
        //ne fait que prendre le message du client pour le prendre
        //retourne un string
        private static String recevoirResultat(Socket client)
        {
            string strData = "";
            try
            {
                byte[] buff = new byte[client.SendBufferSize];
                int bytesRead = sck.Receive(buff);
                byte[] formatted = new byte[bytesRead];
                for (int i = 0; i < bytesRead; i++)
                {
                    formatted[i] = buff[i];
                }
                strData = Encoding.ASCII.GetString(formatted);
            }
            //ne fait rien car le client n'a rien envoyer parcequ'il c'est déconnecter
            catch(SocketException sock){}
            return strData;
        }
        //aperdu() prend un joueur
        //vérifie si tout les bateaux sont coulé
        //retourne vrai ou faux
        private static bool aperdu(Joueur player)
        {
            return porteAvionCoule(player) && ContreTorpilleCoule(player) && SousMarinCoule(player) && CroiseurCoule(player) && TorpilleurCoule(player);
        }
        //porteAvionCoule()prend un joueur
        //vérifie si le porte avion est coulé de ce joueur
        //retourne vrai ou faux
        private static bool porteAvionCoule(Joueur player)
        {
            for (int i = 0; i < player.PorteAvion.longueur; ++i)
            {
                if (player.PorteAvion.estTouche[i] == false)
                    return false;
            }
            return true;
        }
        //ContreTorpilleCoule()prend un joueur
        //vérifie si le Contre Torpille est coulé de ce joueur
        //retourne vrai ou faux
        private static bool ContreTorpilleCoule(Joueur player)
        {
            for (int i = 0; i < player.ContreTorpille.longueur; ++i)
            {
                if (player.ContreTorpille.estTouche[i] == false)
                    return false;
            }
            return true;            
        }
        //SousMarinCoule()prend un joueur
        //vérifie si le Sous Marin est coulé de ce joueur
        //retourne vrai ou faux
        private static bool SousMarinCoule(Joueur player)
        {
            for (int i = 0; i < player.SousMarin.longueur; ++i)
            {
                if (player.SousMarin.estTouche[i] == false)
                    return false;
            }
            return true;
        }
        //CroiseurCoule()prend un joueur
        //vérifie si le Croiseur est coulé de ce joueur
        //retourne vrai ou faux
        private static bool CroiseurCoule(Joueur player)
        {
            for (int i = 0; i < player.Croiseur.longueur; ++i)
            {
                if (player.Croiseur.estTouche[i] == false)
                    return false;
            }
            return true;
        }
        //TorpilleurCoule()prend un joueur
        //vérifie si le Torpilleur est coulé de ce joueur
        //retourne vrai ou faux
        private static bool TorpilleurCoule(Joueur player)
        {
            for (int i = 0; i < player.Torpilleur.longueur; ++i)
            {
                if (player.Torpilleur.estTouche[i] == false)
                    return false;
            }
            return true;
        }
    }
}
