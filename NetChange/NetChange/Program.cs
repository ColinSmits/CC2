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
        static int nrOfNbs;
        static Thread[] threads;
        static int[] neighbours;
        static int[] connection;
        static int[] distances;

        static void Main(string[] args)
        {
            connection = new int[19];
            neighbours = new int[19];
            distances = new int[20];
            
            ownPort = args[0];
            Console.Title = "NetChange" + ownPort;
            nrOfNbs = args.Length - 2;

            for (int i = 1; i < args.Length; i++)
            {
                int port = int.Parse(args[i]);
                neighbours[port - 555000] = i;
                distances[port - 555000] = int.MaxValue; 
                connection[i] = port;
            }
            distances[int.Parse(ownPort) - 555000] = 0;
 

            threads = new Thread[21];
            threads[0] = new Thread(consoleHandler);
            threads[1] = new Thread(connectionHandler);
            for (int i = 2; i < args.Length; i++)
            {
                threads[i] = new Thread(communicationHandler);
            }

            /*for (int x = 1; x < nrOfNbs + 2; x++)
            {
                Console.WriteLine("threads[" + x + "] geeft weer poort " + connection[x]);
                Console.WriteLine("De afstand tot " + connection[x] + " = " + distances[x]);
            }
            */
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
                    nrOfNbs++;
                    int poortnr = int.Parse(words[1]);
                    threads[2 + nrOfNbs] = new Thread(communicationHandler);
                    neighbours[poortnr - 555000] = nrOfNbs + 2;
                    //threads[2+nrofNbs].Start(..)
                    sendMsg(poortnr, "Connected" + ownPort);
                }

                else if (prog == "D")
                {
                    int poortnr = int.Parse(words[1]);
                    sendMsg(poortnr, "Disconnect" + ownPort);
                    int own, last;
                    own = neighbours[poortnr - 555000];
                    last = neighbours[nrOfNbs + 555000];

                    threads[own] = threads[last];
                    threads[last] = null;
                    nrOfNbs--;
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

        private static void communicationHandler()
        {

        }



        private static void connectionHandler()
        {


        }




    }

    class Client
    {
        Client(object t)
        {
            try
            {
                TcpClient client = new TcpClient("localhost", 1234);
                StreamReader clientIn = new StreamReader(client.GetStream());
                StreamWriter clientOut = new StreamWriter(client.GetStream());

                clientOut.AutoFlush = true;

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
        Server(object t)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 1234);
            try
            {
                server.Start();
                TcpClient client = server.AcceptTcpClient();
                StreamReader clientIn = new StreamReader(client.GetStream());
                StreamWriter clientOut = new StreamWriter(client.GetStream());
                clientOut.AutoFlush = true;
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
