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
        static TcpClient[] client;
        static TcpListener server;
        static StreamWriter[] streamsOut;
        static string ownPort;
        static int ownPortInt;
        static int nrOfNbs;
        static Thread[] threads;
        static List<int> nbPorts;
        static int[] connection;
        static int[] distances;

        static void Main(string[] args)
        {
            //initializing arrays/lists
            streamsOut = new StreamWriter[19];
            client = new TcpClient[19];
            connection = new int[19];
            nbPorts = new List<int>();
            distances = new int[20];

            //assigning own port number
            ownPort = args[0];
            ownPortInt = int.Parse(ownPort);
            Console.Title = "NetChange " + ownPort;


            nrOfNbs = args.Length - 2;
            threads = new Thread[21];
            //starting threads for console handling and new connections
            threads[0] = new Thread(consoleHandler);
            threads[0].Start();
            threads[1] = new Thread(connectionHandler);
            threads[1].Start();

            //starting the server
            server = new TcpListener(IPAddress.Any, ownPortInt);
            server.Start();

            //Looping through neighbours and assigning either as a client to the neighbour, or accepting the neighbour as a client
            for (int i = 1; i < args.Length; i++)
            {

                int port = int.Parse(args[i]);
                int calcPort = port - 55500;
                nbPorts.Add(i);
                distances[calcPort] = int.MaxValue;
                connection[i] = port;
                if (port < ownPortInt)
                {
                    threads[calcPort + 2] = new Thread(createClient);
                    threads[calcPort + 2].Start(port);
                    Console.WriteLine("Succesfully created Client for port " + port + " on thread " + (calcPort + 2));


                }
                else
                {
                    
                    threads[calcPort + 2] = new Thread(acceptClient);
                    threads[calcPort + 2].Start();
                    
                }

            }

            //STILL TO DO: Join all active threads


            distances[ownPortInt - 55500] = 0;


            Console.ReadKey();
        }

        private static void acceptClient()
        {
            
            //accepting client and making instances of the streams to read and write within the thread and socket
            TcpClient newclient = server.AcceptTcpClient();
            StreamReader streamIn = new StreamReader(newclient.GetStream());
            StreamWriter streamOut = new StreamWriter(newclient.GetStream());
            streamOut.AutoFlush = true;
            Console.WriteLine("Succesfully created Server");
            while (true)
            {
                string msg = streamIn.ReadLine();

                string[] parts = msg.Split(' ');
                if (parts[0] == "Connection:")
                {
                    int remoteport = int.Parse(parts[1]) - 55500;
                    streamsOut[remoteport] = streamOut;

                    Console.WriteLine("stream connected to port");
                    sendMsg(remoteport + 55500, "Here i am");
                }

                Console.WriteLine("Message Received");
                Console.WriteLine(msg);
                streamOut.WriteLine(msg);
            }
        }

        private static void consoleHandler()
        {
            //handling the operations which can be used as input in the console
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                string[] words = line.Split(' ');
                string prog = words[0];
                if (prog == "R")
                {
                    Console.WriteLine("Hier komt dan de routing table");
                }

                else if (prog == "B")
                {
                    int poortnr = int.Parse(words[1]);
                    string msg = words[2];
                    sendMsg(poortnr, msg);
                }


                //still to implement!! (when server, when client)
                else if (prog == "C")
                {
                    int poortnr = int.Parse(words[1]);
                    if (poortnr > ownPortInt)
                    {
                        threads[poortnr] = new Thread(createClient);
                        threads[poortnr].Start(poortnr);
                    }
                    else
                    {
                        threads[poortnr] = new Thread(acceptClient);
                        threads[poortnr].Start();
                    }



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
        //Working sending messages over the socket using the stream according to the portnr
        private static void sendMsg(int poortnr, string msg)
        {

            streamsOut[poortnr - 55500].WriteLine(msg);
            Console.WriteLine("Message Sent");


        }

        private static void showRoutingTable()
        {
            throw new NotImplementedException();
        }


        private static void connectionHandler()
        {

            throw new NotImplementedException();
        }

        

        private static void createClient(object t)
        {
            
            int portnr = (int)t;
            bool connected = false;
            Console.WriteLine("stage 1");
            //keep connecting until server reacts. If failed, wait 10 ms before doing again
            try
            {
                while (!connected)
                {
                    Console.WriteLine("still Trying");
                    try
                    {
                        //create client on the server identified by portnr
                        client[portnr - 55500] = new TcpClient("localhost", portnr);
                        connected = true;
                        Console.WriteLine("connected = true");
                    }
                    catch { Thread.Sleep(10); }
                }
                Console.WriteLine("connected");

                //create the streams for the current connection
                StreamReader streamIn = new StreamReader(client[portnr - 55500].GetStream());
                StreamWriter streamOut = new StreamWriter(client[portnr - 55500].GetStream());

                streamOut.AutoFlush = true;
                streamOut.WriteLine("Connection: " + ownPort);

                //assign output stream to portnr in the array for use in sendMsg
                streamsOut[portnr - 55500] = streamOut;
                while (true)
                {
                    string msg = streamIn.ReadLine();
                    if (msg == "Here i am")
                    {
                        Console.WriteLine("Oh, i got you too");
                    }
                    Console.WriteLine("Message Received");

                    //Implement message handling here (updates on routing table, mydist, etc.)

                }
            }
            catch
            { }
        }

    }


}
