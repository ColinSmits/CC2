/* Written by:
 * Kylian Kuijer, 4055195
 * Colin Smits,   4075390
 * Concurrency Pract 2
 * 
 * 
 * Notes: - 4 reports are sent along, to show the repetitive correctness of the program 
 *          (except for the variable limit) *  
 */ 

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
        static TcpListener server;
        static StreamWriter[] streamsOut;
        static StreamReader[] streamsIn;
        static string ownPort;
        static int ownPortInt;
        static Thread[] threads;
        static List<int> nbPorts;
        static int[,] ndis;
        static int[] distances;
        static int[] preferred;
        static int max;

        //locks used for the different lists/arrays to ensure specific locking
        private static object ndisLock = new Object(), distLock = new Object(), prefLock = new Object(), listlock = new Object(), calcLock = new Object();

        //Async server
        public static ManualResetEvent allDone = new ManualResetEvent(false),
                                       connectDone = new ManualResetEvent(false);


        static void Main(string[] args)
        {
            //initializing arrays/lists
            streamsOut = new StreamWriter[19];
            streamsIn = new StreamReader[19];
            max = 20;
            nbPorts = new List<int>();
            distances = new int[20];
            preferred = new int[20];
            ndis = new int[20, 20];
            threads = new Thread[22];

            //assigning own port number
            ownPort = args[0];
            ownPortInt = int.Parse(ownPort);
            Console.Title = "NetChange " + ownPort;


            //starting threads for console handling and new connections
            threads[20] = new Thread(consoleHandler);
            threads[20].Start();
            threads[21] = new Thread(connectionHandler);
            threads[21].Start();

            //starting the server
            server = new TcpListener(IPAddress.Any, ownPortInt);
            server.Start();

            //assigning maxvalue to all distances/ndis
            for (int x = 0; x < distances.Length; x++)
            {
                distances[x] = max;
                for (int y = 0; y < ndis.GetLength(1); y++)
                {
                    ndis[x, y] = max + 1;
                }
            }
            ndis[ownPortInt - 55500, ownPortInt - 55500] = -1;
            distances[ownPortInt - 55500] = 0;
            preferred[ownPortInt - 55500] = ownPortInt;

            //Looping through neighbours and assigning either as a client to the neighbour, or accepting the neighbour as a client
            for (int i = 1; i < args.Length; i++)
            {

                int port = int.Parse(args[i]);
                int calcPort = port - 55500;
                distances[calcPort] = 1; //Neighbour
                ndis[ownPortInt - 55500, calcPort] = 1;
                ndis[calcPort, ownPortInt - 55500] = 1;
                if (port < ownPortInt)
                {
                    threads[calcPort] = new Thread(asyncCreate);
                    threads[calcPort].Start(port);
                }

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
                    showRoutingTable();
                }
                //sending messages
                else if (prog == "B")
                {
                    int poortnr = int.Parse(words[1]);
                    if (poortnr < 55500 || poortnr > 55519 || preferred[poortnr - 55500] < 55500)
                    {
                        Console.WriteLine("Poort " + poortnr + " is niet bekend");
                    }
                    else
                    {
                        string msg = "MSG" + " " + poortnr + " " + words[2];
                        sendMsg(poortnr, msg);
                    }
                }


                //handling new connection
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
                        threads[poortnr - 55500] = new Thread(asyncGetConn);
                        threads[poortnr - 55500].Start(poortnr);
                    }
                }
                //handling disconnect
                else if (prog == "D")
                {
                    int poortnr = int.Parse(words[1]);
                    bool contain = false;
                    lock (listlock)
                    {
                        contain = nbPorts.Contains(poortnr);
                    }
                    if (!contain)
                    {
                        Console.WriteLine("Poort " + poortnr + " is niet bekend");
                    }

                    else
                    {
                        //send msg disconnect so that other port closes connection as well
                        sendMsg(poortnr, "Disconnect: " + ownPort);

                        disCon(poortnr);
                        threads[poortnr - 55500].Abort();
                    }
                }


            }

        }

        private static void disCon(int poortnr)
        {
            Console.WriteLine("Verbroken: " + poortnr);
            int recalcport = poortnr - 55500;
            //remove port from nb's 
            lock (listlock)
            {
                nbPorts.Remove(poortnr);
            }
            //set all ndis from the disconnected port to max + 1 (unreachable)
            for (int x = 0; x < max; x++)
            {
                ndis[recalcport, x] = max + 1;
            }
            //set own dis to unreachable and recalculate
            ndis[ownPortInt - 55500, recalcport] = max + 1;
            string[] s = new string[4];
            s[1] = poortnr + "";
            s[2] = max + "";
            s[3] = ownPortInt + "";
            calcDist(s);
            //check if disconnected port was preferred for another. If so: recalculate this port.
            for (int y = 0; y < max; y++)
            {
                if (preferred[y] == poortnr)
                {

                    s[1] = (y + 55500) + "";
                    s[2] = (max + 1) + "";
                    s[3] = ownPortInt + "";
                    calcDist(s);
                }
            }

        }


        //Working sending messages over the socket using the stream according to the portnr
        private static void sendMsg(int poortnr, string msg)
        {
            lock (prefLock)
            {
                streamsOut[preferred[poortnr - 55500] - 55500].WriteLine(msg);
            }
        }

        // show routing table while locking the distances 
        private static void showRoutingTable()
        {
            for (int x = 0; x < 20; x++)
            {
                if (distances[x] < max)
                {
                    string pref = preferred[x] + "";
                    if (distances[x] == 0)
                        pref = "local";
                    lock (distLock)
                    {
                        Console.WriteLine((55500 + x) + " " + distances[x] + " " + pref);
                    }
                }
            }


        }
        //method for the communication between threads (therefore while (true))
        private static void communicationHandler(object obj)
        {
            int portnr = (int)obj;
            StreamReader streamIn = streamsIn[portnr];
            while (true)
            {
                string msg = streamIn.ReadLine();
                string[] parts = msg.Split(' ');
                if (parts[0] == "MYDIST:")
                {
                    //my dist so recalc. Lock used to ensure FIFO
                    lock (calcLock)
                    {
                        calcDist(parts);
                    }
                }
                else if (parts[0] == "MSG")
                {
                    if (parts[1] == ownPort)
                    {
                        //show message on screen
                        Console.WriteLine(parts[2]);
                    }
                    else
                    {
                        //forward message
                        int toPort = int.Parse(parts[1]);
                        sendMsg(toPort, msg);
                        Console.WriteLine("Bericht voor " + toPort + " doorgestuurd naar " + preferred[toPort - 55500]);
                    }
                }
                else if (parts[0] == "Disconnect:")
                {
                    //handling disconnect send from other port (which received D portnr)
                    int port = int.Parse(parts[1]);
                    disCon(port);
                }
            }
        }
    
        private static void calcDist(string[] parts)
        {
            int changedP = int.Parse(parts[1]) - 55500;
            int newDis = int.Parse(parts[2]);
            int newPort = int.Parse(parts[3]) - 55500;
            if (newPort != changedP)
            {
                //only if not your own port, set the ndis
                lock (ndisLock)
                {
                    ndis[newPort, changedP] = newDis;
                }
            }

            if (changedP != ownPortInt - 55500)
            {
                int lowDist = max + 1;
                int pref = 0;
                for (int x = 0; x < max; x++)
                {
                    //check for lowest distance and corresponding portnr
                    if (ndis[x, changedP] < lowDist)
                    {
                        lowDist = ndis[x, changedP];
                        pref = x + 55500;
                    }
                }

                if (lowDist >= max)
                {
                    //distance is >20, so unreachable
                    Console.WriteLine("Onbereikbaar: " + parts[1]);

                    lock (ndisLock)
                    {
                        //unreachable, so all ndis are set to max
                        for (int n = 0; n < max; n++)
                        {
                            ndis[changedP, n] = max + 1;
                        }
                    }
                    if (lowDist <= max && distances[changedP] != max)
                    {
                        sendMyDist(changedP + 55500, 20);
                    }
                    //setting new distance and preferred using locks
                    lock (distLock)
                    {
                        distances[changedP] = max;
                    }
                    lock (prefLock)
                    {
                        preferred[changedP] = 0;
                    }

                    //check again if the unreachable port was a preferred port for another port
                    for (int y = 0; y < max; y++)
                    {
                        if (preferred[y] == changedP + 55500)
                        {
                            string[] s = new string[4];
                            s[1] = (y + 55500) + "";
                            s[2] = (max + 1) + "";
                            s[3] = ownPortInt + "";
                            calcDist(s);
                        }
                    }
                }

                else
                {
                    if (lowDist == max)
                    {
                        lowDist--;
                    }
                    bool same = (lowDist + 1 == distances[changedP]);
                    //if distance or preferred has changed: set new distance and preferred using locks and send MyDist 
                    if (!same || pref != preferred[changedP])
                    {
                        lock (distLock)
                        {
                            distances[changedP] = lowDist + 1;
                        }
                        lock (prefLock)
                        {
                            preferred[changedP] = pref;
                        }
                        if (!same)
                        {

                            Console.WriteLine("Afstand naar " + parts[1] + " is nu " + distances[changedP] + " via " + preferred[changedP]);


                            sendMyDist(changedP + 55500, distances[changedP]);
                        }
                    }
                }


            }

        }


        //send mydist to all neighbourports
        private static void sendMyDist(int poortnr, int newdistance)
        {
            lock (listlock)
            {
                foreach (int port in nbPorts)
                {
                    sendMsg(port, "MYDIST: " + poortnr + " " + newdistance + " " + ownPort);
                }
            }

        }

        //at start connection: send all your current known distances to known ports to the new connected port
        private static void sendAllDist(int poortnr)
        {
            lock (distLock)
            {
                for (int x = 0; x < distances.Length; x++)
                {
                    if (distances[x] < max)
                    {
                        sendMsg(poortnr, "MYDIST: " + (x + 55500) + " " + distances[x] + " " + ownPort);
                    }
                }
            }
        }

        #region Socket Operations
        //socket listener during whole execution
        private static void connectionHandler()
        {
            while (true)
            {
                try
                {
                    allDone.Reset();

                    server.BeginAcceptTcpClient(new AsyncCallback(asyncAcceptCB), server);
                    allDone.WaitOne();
                }
                catch
                {
                    Thread.Sleep(10);
                }
            }
        }

        
        private static void asyncAccept()
        {
            try
            {

                server.BeginAcceptTcpClient(new AsyncCallback(asyncAcceptCB), server);
                allDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //Callback method for accepting
        private static void asyncAcceptCB(IAsyncResult ar)
        {
            allDone.Set();
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient accClient = listener.EndAcceptTcpClient(ar);

            
            StreamReader streamIn = new StreamReader(accClient.GetStream());
            StreamWriter streamOut = new StreamWriter(accClient.GetStream());

            streamOut.AutoFlush = true;
            streamOut.WriteLine("//Portnr: " + ownPort);

            bool obtained = false;
            while (!obtained)
            {
                string line = streamIn.ReadLine();
                string[] parts = line.Split(' ');
                if (parts[0] == "//Portnr:")
                {
                    int portnr = int.Parse(parts[1]) - 55500;
                    Console.WriteLine("Verbonden: " + parts[1]);
                    //write streams to array for use in sending msg
                    streamsOut[portnr] = streamOut;
                    streamsIn[portnr] = streamIn;

                    //set all distances, ndis and preferred (neighb, so dist 1)
                    distances[portnr] = 1;

                    preferred[portnr] = portnr + 55500;

                    //locks are used here to guarantee that the new values are used
                    lock (ndisLock)
                    {
                        ndis[ownPortInt - 55500, portnr] = 1;
                        ndis[portnr, portnr] = 0;
                        ndis[portnr, ownPortInt - 55500] = 1;
                    }

                    lock (listlock)
                    {
                        nbPorts.Add(portnr + 55500);
                    }

                    threads[portnr] = new Thread(communicationHandler);
                    threads[portnr].Start(portnr);

                    sendMyDist(portnr + 55500, 1);
                    obtained = true;
                    sendAllDist(portnr + 55500);
                }
                //Only happens when first connection was from low to high (switch server and client)
                if (parts[0] == "//PleasePortnr:")
                {
                    int portnr = int.Parse(parts[1]) - 55500;
                    threads[portnr] = new Thread(asyncCreate);
                    threads[portnr].Start(portnr + 55500);
                    streamOut.WriteLine("//Close");
                    obtained = true;
                }
            }

        }

        private static void asyncCreate(object t)
        {
            int portnr = (int)t;

            try
            {
                //create client on the server identified by portnr

                TcpClient client = new TcpClient();

                client.BeginConnect("localhost", portnr, new AsyncCallback(asyncCreateCB), client);
                connectDone.WaitOne();
                Console.WriteLine("Verbonden: " + portnr);


            }
            catch { }


        }
        //Create callback
        private static void asyncCreateCB(IAsyncResult ar)
        {
            try
            {
                TcpClient client = (TcpClient)ar.AsyncState;
                client.EndConnect(ar);


                connectDone.Set();
                StreamReader streamIn = new StreamReader(client.GetStream());
                StreamWriter streamOut = new StreamWriter(client.GetStream());

                streamOut.AutoFlush = true;
                streamOut.WriteLine("//Portnr: " + ownPortInt);

                bool obtained = false;
                while (!obtained)
                {
                    string line = streamIn.ReadLine();
                    string[] parts = line.Split(' ');
                    if (parts[0] == "//Portnr:")
                    {
                        int portnr = int.Parse(parts[1]) - 55500;
                        //write streams to array for use in sending msg
                        streamsOut[portnr] = streamOut;
                        streamsIn[portnr] = streamIn;

                        //set all distances, ndis and preferred (neighb, so dist 1)
                        distances[portnr] = 1;

                        preferred[portnr] = portnr + 55500;

                        //locks are used here to guarantee that the new values are used
                        lock (ndisLock)
                        {
                            ndis[ownPortInt - 55500, portnr] = 1;
                            ndis[portnr, portnr] = 0;
                            ndis[portnr, ownPortInt - 55500] = 1;
                        }
                        lock (listlock)
                        {
                            nbPorts.Add(portnr + 55500);
                        }

                        threads[portnr] = new Thread(communicationHandler);
                        threads[portnr].Start(portnr);

                        sendMyDist(portnr + 55500, 1);
                        obtained = true;
                        sendAllDist(portnr + 55500);
                    }
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //Method for a connection offer from a lower port to a higher (have to be switched)
        private static void asyncGetConn(object obj)
        {
            int portnr = (int)obj;

            try
            {
                

                TcpClient client = new TcpClient();

                client.BeginConnect("localhost", portnr, new AsyncCallback(asyncGetConnCB), client);
                connectDone.WaitOne();
                Console.WriteLine("Verbonden: " + portnr);
            }
            catch { }


        }

        //Ask other port to connect to server on this port
        private static void asyncGetConnCB(IAsyncResult ar)
        {
            try
            {
                TcpClient client = (TcpClient)ar.AsyncState;
                client.EndConnect(ar);


                connectDone.Set();
                StreamReader streamIn = new StreamReader(client.GetStream());
                StreamWriter streamOut = new StreamWriter(client.GetStream());

                streamOut.AutoFlush = true;
                streamOut.WriteLine("//PleasePortnr: " + ownPortInt);

                bool verified = false;
                while (!verified)
                {
                    string line = streamIn.ReadLine();
                    string[] parts = line.Split(' ');
                    if (parts[0] == "//Close")
                    {
                        verified = true;
                    }

                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

        #endregion


}
