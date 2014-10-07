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

        static void Main(string[] args)
        {
            Console.Title = "NetChange" + args[0];
            Thread[] threads;
                      

            threads = new Thread[3];
            threads[0] = new Thread(consoleHandler);
            threads[1] = new Thread(connectionHandler);
            //threads[2..] = new Thread(communicationHandler)

        }

        private static void consoleHandler()
        {

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
