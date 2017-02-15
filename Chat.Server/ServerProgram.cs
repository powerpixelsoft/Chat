﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Server
{
    using System.Collections.Concurrent;
    using System.Configuration;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Windows.Forms;
    using Utils;

    internal class ServerProgram
    {
        private const int PORT = 1473;
        private const String IP = "192.168.178.28";
        private static TcpListener serverListener;

        private static Boolean isAlive = true, isBroadcasting = true, acceptingMessages = true;
        private static Object broadcastLock, addClientLock, removeClientLock;

        private static Thread acceptConnectionsThread;
        private static List<Thread> clientsThreads;

        private static ConcurrentStack<Packet> packetStack;
        private static ThreadSafeCollection<Client> clients;

        private static ILogger logger;

        //todo: crash! if client is stopped on a breakpoint
        //todo: input connection details
        //todo: commands understanding
        //todo: cleanup!
        //todo: check if a client with the same name already exists, so something about it

        static void Main(String[] args)
        {
            logger = new FileLogger(@"C:\Temp\PopeChat\Server.txt");

            try
            {
                serverListener = StartServer
                (
                    ConfigurationManager.AppSettings["default_ip"],
                    Convert.ToInt32(ConfigurationManager.AppSettings["default_port"])
                );
                Setup();

                //accept connections
                acceptConnectionsThread = new Thread(AcceptConnections);
                acceptConnectionsThread.Start();

                Broadcast();

                serverListener?.Stop();
            }
            catch (Exception e)
            {
                logger.Write("Main crashed", e);
                Console.WriteLine(e);
                throw;
            }
        }

        private static TcpListener StartServer(String ip, int port)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(ip);
                TcpListener listener = new TcpListener(ipAddress, port);
                listener.Start();

                Log.WriteSystem("ServerProgram is started.");
                Log.WriteSystem($"End point: {listener.LocalEndpoint}");

                return listener;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private static void Setup()
        {
            addClientLock = new Object();
            removeClientLock = new Object();
            broadcastLock = new Object();

            clientsThreads = new List<Thread>();
            packetStack = new ConcurrentStack<Packet>();
            clients = new ThreadSafeCollection<Client>();
        }

        private static void AcceptConnections()
        {
            Log.WriteSystem("[Listen] thread is ON");

            while (isAlive)
            {
                try
                {
                    Log.WriteSystem("Awaiting connections...");
                    Socket newSocket = serverListener.AcceptSocket();

                    Packet packet = ReceivePacket(newSocket);
                    //AddClient(newSocket, packet);

                    Client client = new Client(packet.ClientName, newSocket);
                    AddClient(client);

                    packetStack.Push(packet);
                }
                catch (Exception e)
                {
                    logger.Write("Accept connections crashed", e);
                    Console.WriteLine(e);
                }
            }
        }
        private static void AcceptMessages(Client client)
        {
            Log.WriteSystem($"Started accepting messages from @{client.Name}");

            while (acceptingMessages)
            {
                try
                {
                    Packet packet = ReceivePacket(client.Socket);

                    if (packet != null)
                    {
                        if (packet.Flag.Equals("end"))
                        {
                            Log.WriteMessage(packet.ClientName, packet.Message);
                            RemoveClient(client);
                        }
                        else
                        {
                            Log.WriteMessage(packet.ClientName, packet.Message);
                            packetStack.Push(packet);
                        }
                    }
                }
                catch (SocketException e)
                {
                    RemoveClient(client);
                    break;
                }
            }
        }
        private static void Broadcast()
        {
            Log.WriteSystem("[Broadcast] thread is ON");

            while (isBroadcasting)
            {
                //if (clients.Count == 0) continue;
                if (clients.Count == 0) continue;

                if (packetStack.Count > 0)
                {

                    Packet topPacket;
                    packetStack.TryPop(out topPacket);
                    if(topPacket == null) continue;

                    try
                    {
                        lock (broadcastLock)
                        {
                            foreach (Client client in clients)
                            {
                                if (!client.Socket.Connected)
                                {
                                    RemoveClient(client);
                                    continue;
                                }
                                SendPacket(client.Socket, topPacket);
                            }
                        }
                    }
                    catch (Exception e) { Console.Write(e); }
                }
            }
        }
        private static Packet ReceivePacket(Socket socket)
        {
            Packet packet = new Packet();

            Byte[] expectedBytes = new Byte[ConnectionData.BUFFER_MAX_SIZE];
            Int32 receivedLength = socket.Receive(expectedBytes);

            Byte[] receivedBytes = new Byte[receivedLength];
            Buffer.BlockCopy(expectedBytes, 0, receivedBytes, 0, receivedLength);

            packet = Packet.Decode(receivedBytes);

            return packet;
        }
        private static void SendPacket(Socket socket, Packet packet)
        {
            try
            {
                if(packet != null)
                    socket.Send(packet.Encode());
                else Console.Write("Null Packet was received");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private static void AddClient(Client client)
        {
            lock (addClientLock)
            {
                clients.Add(client);

                Thread clienThread = new Thread(() => AcceptMessages(client));
                clienThread.IsBackground = true;
                clientsThreads.Add(clienThread);
                clienThread.Start();

                Log.WriteSystem($"Connected: {client.Name}");
            }
        }
        private static void RemoveClient(Client client)
        {
            lock (removeClientLock)
            {
                if (clients.Contains(client))
                {
                    Boolean result = clients.Remove(client);
                    if (result)
                    {
                        Log.WriteSystem($"Disconnected: @{client.Name}");

                        if (clients.Count > 0)
                            packetStack.Push(new Packet { ClientName = client.Name, Message = "Is Offline...", Ip = "Null"});
                    }
                }
            }
        }
    }
}

