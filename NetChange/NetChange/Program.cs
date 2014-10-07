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
        static StreamReader[] streamsIn;
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
            streamsIn = new StreamReader[19];
            streamsOut = new StreamWriter[19];
            client = new TcpClient[19];
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
                int calcPort = port - 55500;
                nbPorts.Add(i);
                distances[calcPort] = int.MaxValue;
                connection[i] = port;
                if (port > ownPortInt)
                {
                    threads[calcPort + 2] = new Thread(createClient);
                    threads[calcPort + 2].Start(port);
                    Console.WriteLine("Succesfully created Client for port " + port + " on thread " + (calcPort + 2));
                }
                else
                {
                    threads[calcPort + 2] = new Thread(createServer);
                    threads[calcPort + 2].Start(ownPortInt);
                    Console.WriteLine("Succesfully created Server for port " + port + " on thread " + (calcPort + 2));
                }
                
               
                threads[calcPort + 2].Join();
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
                    Console.WriteLine("Hier komt dan de routing table");
                }

                else if (prog == "B")
                {
                    int poortnr = int.Parse(words[1]);
                    string msg = words[2];
                    //sendMsg(poortnr, msg);
                }

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
                        threads[poortnr] = new Thread(createServer);
                        threads[poortnr].Start(poortnr);
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


        }




    



   
        private static void createClient(object t)
        {
            int portnr = (int)t;
            bool connected = false;
            Console.WriteLine("stage 1");
            
            try
            {
                while (!connected)
                {
                    Console.WriteLine("still Trying");
                    try
                    {
                        client[portnr - 55500] = new TcpClient("localhost", portnr);  
                        connected = true;
                        Console.WriteLine("connected = true");
                    }
                    catch { Thread.Sleep(10); }
                }
                Console.WriteLine("connected");
                          
                
                streamsIn[portnr - 55500] = new StreamReader(client[portnr - 55500].GetStream());
                streamsOut[portnr - 55500] = new StreamWriter(client[portnr - 55500].GetStream());

                streamsOut[portnr - 55500].AutoFlush = true;
                while (true)
                {
                    streamsOut[portnr - 55500].WriteLine(Console.ReadLine());
                    Console.WriteLine(streamsIn[portnr - 55500].ReadLine());
                }
            }
            catch
            { }
        }
    

  
        private static void createServer(object t)
        {
            Console.WriteLine("Starting created Server");
            int portnr = (int) t ; 
            TcpListener server = new TcpListener(IPAddress.Any, portnr);
            try
            {
                Console.WriteLine("Still Trying");
                server.Start();
                Console.WriteLine("Waiting for connection");
                
                Console.WriteLine("connection established");
                TcpClient newclient = server.AcceptTcpClient();
                Console.WriteLine(((IPEndPoint)newclient.Client.RemoteEndPoint).Address);
                Console.WriteLine(((IPEndPoint)newclient.Client.RemoteEndPoint).Port);
                int remoteport = ((IPEndPoint)newclient.Client.RemoteEndPoint).Port;
                client[remoteport - 55500] = newclient;
                streamsIn[remoteport - 55500] = new StreamReader(client[remoteport - 55500].GetStream());
                streamsOut[remoteport - 55500] = new StreamWriter(client[remoteport - 55500].GetStream());

                streamsOut[portnr - 55500].AutoFlush = true;
                Console.WriteLine("Succesfully created Server");
                while (true)
                {
                    string msg = streamsIn[portnr-55500].ReadLine();
                    Console.WriteLine("Message Received");
                    Console.WriteLine(msg);
                    streamsOut[portnr - 55500].WriteLine(msg);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Server could not start");
                Console.WriteLine(e);
                
            }
        }
    }


}
