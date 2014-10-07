using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace NetChange
{
    class Program
    {
        static string ownPort;
        static int ownPortInt;
        static int nrOfNbs;
        static Thread[] threads;
        static List<int> nbPorts;
        static int[] connection;
        static int[] distances;

        static void Main(string[] args)
        {
            connection = new int[19];
            nbPorts = new List<int>();
            distances = new int[20];

            ownPort = args[0];
            ownPortInt = int.Parse(ownPort);
            Console.Title = "NetChange " + ownPort;
            nrOfNbs = args.Length - 2;
            threads = new Thread[21];
            threads[0] = new Thread(consoleHandler);
            threads[0].Start();
            threads[1] = new Thread(connectionHandler);
            threads[1].Start();
            for (int i = 1; i < args.Length; i++)
            {
                int port = int.Parse(args[i]);
                nbPorts.Add(i);
                distances[port - 55500] = int.MaxValue;
                connection[i] = port;

                threads[port - 55500] = new Thread(communicationHandler);
                threads[port - 55500].Start(port);
                threads[port - 55500].Join();
            }
            distances[ownPortInt - 55500] = 0;


            Console.ReadKey();
        }

        private static void consoleHandler()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                string[] words = line.Split(' ');
                string prog = words[0];
                if (prog == "R")
                {
                    showRoutingTable();
                }

                else if (prog == "B")
                {
                    int poortnr = int.Parse(words[1]);
                    string msg = words[2];
                    sendMsg(poortnr, msg);
                }

                else if (prog == "C")
                {
                    int poortnr = int.Parse(words[1]);
                    threads[poortnr] = new Thread(communicationHandler);

                    threads[poortnr].Start(poortnr);

                    nbPorts.Add(poortnr);

                    sendMsg(poortnr, "Connected" + ownPort);
                }

                else if (prog == "D")
                {
                    int poortnr = int.Parse(words[1]);
                    sendMsg(poortnr, "Disconnect" + ownPort);
                    threads[poortnr - 55500 + 2].Abort();
                }
            }

        }

        private static void sendMsg(int poortnr, string msg)
        {
            throw new NotImplementedException();
        }

        private static void showRoutingTable()
        {
            throw new NotImplementedException();
        }

        private static void communicationHandler(object t)
        {
            int port = (int)t;
            if (port > ownPortInt)
            {
                Client client = new Client(port);
                
            }
            else
            {
                Server server = new Server(port);
                
            }

        }



        private static void connectionHandler()
        {


        }




    }



    class Client
    {
        public Client(int portnr)
        {
            bool connected = false;
            TcpClient client;
            TcpClient client1;
            try
            {
                while (!connected)
                {
                    try
                    {
                       
                        client1 = new TcpClient("localhost", portnr);
                        connected = true;
                    }
                    catch { Thread.Sleep(10); }
                }
                client = new TcpClient("localhost", portnr);            

                StreamReader clientIn = new StreamReader(client.GetStream());
                StreamWriter clientOut = new StreamWriter(client.GetStream());

                clientOut.AutoFlush = true;
                Console.WriteLine("Succesfully created Client");

                while (true)
                {
                    clientOut.WriteLine(Console.ReadLine());
                    Console.WriteLine(clientIn.ReadLine());
                }
            }
            catch
            { }
        }
    }

    class Server
    {
        public Server(int portnr)
        {
            TcpListener server = new TcpListener(IPAddress.Any, portnr);
            try
            {
                server.Start();
                TcpClient client = server.AcceptTcpClient();
                StreamReader clientIn = new StreamReader(client.GetStream());
                StreamWriter clientOut = new StreamWriter(client.GetStream());
                clientOut.AutoFlush = true;
                Console.WriteLine("Succesfully created Server");
                while (true)
                {
                    string msg = clientIn.ReadLine();
                    Console.WriteLine(msg);
                    clientOut.WriteLine(msg);
                }
            }
            catch
            { }
        }
    }


}
