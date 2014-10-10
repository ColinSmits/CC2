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
        static StreamReader[] streamsIn;
        static string ownPort;
        static int ownPortInt;
        static int nrOfNbs;
        static Thread[] threads;
        static List<int> nbPorts;
        static int[] connection;
        static int[] distances;

        //Async server
        public static ManualResetEvent allDone = new ManualResetEvent(false), 
                                       connectDone = new ManualResetEvent(false), 
                                       
                                       sendDone = new ManualResetEvent(false),
                                       receiveDone = new ManualResetEvent(false);




        static void Main(string[] args)
        {
            //initializing arrays/lists
            streamsOut = new StreamWriter[19];
            streamsIn = new StreamReader[19];
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
                distances[calcPort] = 1; //Neighbour
                connection[i] = port;
                if (port < ownPortInt)
                {
                    threads[calcPort + 2] = new Thread(asyncCreate);
                    threads[calcPort + 2].Start(port);
                   
                }
                else
                {
                    
                    threads[calcPort + 2] = new Thread(asyncAccept);
                    threads[calcPort + 2].Start();
                    
                }

            }

            //STILL TO DO: Join all active threads


            distances[ownPortInt - 55500] = 0;


            Console.ReadKey();
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
                    if (poortnr < ownPortInt)
                    {
                        threads[poortnr - 55500] = new Thread(asyncCreate);
                        threads[poortnr - 55500].Start(poortnr);
                    }
                    else
                    {
                       // threads[poortnr] = new Thread(acceptClient);
                      //  threads[poortnr].Start();
                    }



                    nbPorts.Add(poortnr);

                    //sendMsg(poortnr, "Connected" + ownPort);
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

            streamsOut[poortnr - 55500].WriteLine("Hello There");
            Console.WriteLine("Message Sent");


        }

        private static void showRoutingTable()
        {
           // throw new NotImplementedException();
        }

        private static void connectionHandler()
        {
            while (true)
            {
                try
                {
                    allDone.Reset();
                    Console.WriteLine("Awaiting connection...");
                    server.BeginAcceptTcpClient(new AsyncCallback(asyncAcceptCB), server);
                    allDone.WaitOne();
                }
                catch { }
            }
        }

        
        private static void asyncAccept()
        {
            try {
            Console.WriteLine("Awaiting connection...");
            server.BeginAcceptTcpClient(new AsyncCallback(asyncAcceptCB), server);
            allDone.WaitOne();
            }
            catch (Exception e){
                Console.WriteLine(e);
            }
        }

        private static void asyncAcceptCB(IAsyncResult ar)
        {
            allDone.Set();
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient accClient = listener.EndAcceptTcpClient(ar);
            Console.WriteLine("Client accepted");

            StreamReader streamIn = new StreamReader(accClient.GetStream());
            StreamWriter streamOut = new StreamWriter(accClient.GetStream());

            streamOut.AutoFlush = true;
            streamOut.WriteLine("//Portnr: " + ownPort);
            Console.WriteLine("written to stream");
            bool obtained = false;
            while (!obtained)
            {
                string line = streamIn.ReadLine();
                string[] parts = line.Split(' ');
                if (parts[0] == "//Portnr:")
                {
                    int portnr = int.Parse(parts[1]) - 55500;
                    streamsOut[portnr] = streamOut;
                    streamsIn[portnr] = streamIn;
                    Console.WriteLine("Stream saved");
                    threads[portnr] = new Thread(communicationHandler);
                    threads[portnr].Start(portnr);
                    obtained = true;
                }
            }
            
        }

        private static void communicationHandler(object obj)
        {
            int portnr = (int)obj;
            StreamReader streamIn = streamsIn[portnr];
            while (true)
            {
                string msg = streamIn.ReadLine();
                Console.WriteLine(msg);
            }
        }

        private static void asyncCreate(object t)
        {
            int portnr = (int)t;
          
            Console.WriteLine("stage 1");
           
            try
            {
                //create client on the server identified by portnr
               
               
                TcpClient client = new TcpClient();

                client.BeginConnect("localhost", portnr, new AsyncCallback(asyncCreateCB), client);
                connectDone.WaitOne();
                Console.WriteLine("connected = true");
                

            }
            catch { }
           
           
        }

        private static void asyncCreateCB(IAsyncResult ar)
        {
            try
            {
                TcpClient client = (TcpClient)ar.AsyncState;
                client.EndConnect(ar);
                Console.WriteLine("Connect complete");
                connectDone.Set();
                StreamReader streamIn = new StreamReader(client.GetStream());
                StreamWriter streamOut = new StreamWriter(client.GetStream());

                streamOut.AutoFlush = true;
                streamOut.WriteLine("//Portnr: " + ownPortInt);
                Console.WriteLine("written to stream");
                bool obtained = false;
                while (!obtained)
                {
                    string line = streamIn.ReadLine();
                    string[] parts = line.Split(' ');
                    if (parts[0] == "//Portnr:")
                    {
                        int portnr = int.Parse(parts[1]) - 55500;
                        streamsOut[portnr] = streamOut;
                        streamsIn[portnr] = streamIn;
                        Console.WriteLine("Stream saved");
                        threads[portnr] = new Thread(communicationHandler);
                        threads[portnr].Start(portnr);
                        obtained = true;
                    }
                }

               
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }


}
