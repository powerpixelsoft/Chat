using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Server
{
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class AsyncServer
    {
        private const Int32 PORT = 1473;
        private const String IP = "192.168.178.28";
        private const Int32 BUFFER_SIZE = 0xFFFFFF;
        private const Int32 CONN_MAX = 0xFF;

        private static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsyncServer()
        {
            
        }

        public void StartListen()
        {
            Byte[] buffer = new Byte[BUFFER_SIZE];

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(IP), PORT);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(CONN_MAX);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(ConnectionAcceptedCallback, listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }
            }
            catch (Exception e) {  Console.WriteLine(e); }

            Console.WriteLine("\nEnd is one button click awat...");
            Console.Read();
        }

        private void ConnectionAcceptedCallback(IAsyncResult ar)
        {
            allDone.Set();

            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            StateObject stateObject = new StateObject();
            stateObject.ClientHandlerSocket = handler;
            handler.BeginReceive(stateObject.Buffer, 0, StateObject.BUFFER_SIZE, 0, ReceiveCallback, stateObject);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            StateObject stateObject = (StateObject)ar.AsyncState;
            Socket handler = stateObject.ClientHandlerSocket;

            // Read data from the client socket.   
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                stateObject.DataStringBuilder.Append(Encoding.ASCII.GetString(stateObject.Buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                content = stateObject.DataStringBuilder.ToString();
                if (content.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
                {
                    // All the data has been read from the   
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                    
                    // Echo the data back to the client.  
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(stateObject.Buffer, 0, StateObject.BUFFER_SIZE, 0, ReceiveCallback, stateObject);
                }
            }
        }
        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public class StateObject
    {
        // Packet  socket.  
        public Socket ClientHandlerSocket = null;
        // Size of receive Buffer.  
        public const int BUFFER_SIZE = 0xFFFF;
        // Receive Buffer.  
        public byte[] Buffer = new byte[BUFFER_SIZE];
        // Received data string.  
        public StringBuilder DataStringBuilder = new StringBuilder();
    }
}
