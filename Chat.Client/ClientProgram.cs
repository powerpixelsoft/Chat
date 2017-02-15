using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Client
{
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Chat.Utils;

    internal class ClientProgram
    {
        private static String ip = "192.168.178.28";
        private static Int32 port = 1473;
        private static String name;

        private static TcpClient tcpClient;
        private static Stream clientStream;

        private static Thread sendMessagesThread, receiveMessagesThread;
        private static Boolean isSending = true, isReceiving = true;

        private static Packet packet;


        private static void Main()
        {
            try
            {
                packet = new Packet();
                packet.ClientName = name;

                tcpClient = Connect(ip, port);

                packet.Ip = tcpClient.Client.LocalEndPoint.ToString();
                packet.Message = $"{packet.ClientName} is connected.";
                SendMessage(clientStream, packet);

                receiveMessagesThread = new Thread(ReceiveRoutine);
                receiveMessagesThread.Start();

                SendRoutine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            tcpClient?.Close();
            clientStream?.Close();
        }
        private static TcpClient Connect(String ip, Int32 port)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(ip, port);
                clientStream = client.GetStream();

                Log.WriteSystem("ClientProgram is online");

                return client;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ReceiveRoutine()
        {
            ReceivePacket(clientStream);
        }
        private static void SendRoutine()
        {
            while (isSending)
            {
                String input = Console.ReadLine();

                if (!String.IsNullOrEmpty(input))
                {
                    packet.Message = input;
                    SendMessage(clientStream, packet);
                }
            }
        }
        
        private static void SendMessage(Stream clientStream, Packet packet)
        {
            try
            {
                Byte[] messageBytes = packet.Encode();
                clientStream.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private static void ReceivePacket(Stream clientStream)
        {
            try
            {
                while (isReceiving)
                {
                    Byte[] expectedBytes = new Byte[ConnectionData.BUFFER_MAX_SIZE];
                    Int32 receivedLength = clientStream.Read(expectedBytes, 0, expectedBytes.Length);

                    if (receivedLength == 0) continue;

                    Byte[] receivedBytes = new Byte[receivedLength];
                    Buffer.BlockCopy(expectedBytes, 0, receivedBytes, 0, receivedLength);

                    Packet receivedPacket = Packet.Decode(receivedBytes);
                    Log.WriteMessage(receivedPacket.ClientName, receivedPacket.Message);
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException)
                {
                    Log.WriteSystem("Connection to server was lost...");
                }
                else
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }

        private void GetInput()
        {
            if (String.IsNullOrEmpty(ip))
            {
                Log.WriteSystem("Enter ip:");
                if (!Validation.GetIp(Console.ReadLine(), out ip))
                {
                    Log.WriteSystem("Ip format is wrong");
                    GetInput();
                }
            }

            if (port == 0)
            {
                Log.WriteSystem("Enter port number:");
                if (!Validation.GetPort(Console.ReadLine(), out port))
                {
                    Log.WriteSystem("Port format is wrong");
                    GetInput();
                }
            }

            Log.WriteSystem("Enter Name:");
            String nameInput = Console.ReadLine();
        }
    }
}
