private static void AddClient(Socket socket, Packet packet)
{
    lock (addClientLock)
    {
        clients.TryAdd(socket.RemoteEndPoint.ToString(), socket);
        clientNamesIps.Add(packet.Ip, packet.ClientName);

        Thread clienThread = new Thread(() => AcceptMessages(socket)) { Name = socket.RemoteEndPoint.ToString() };
        clientsThreads.Add(clienThread);
        clienThread.Start();

        Log.WriteSystem($"Connected: {socket.RemoteEndPoint}");
    }
}

private static void AcceptMessages(Socket socket)
{
    Log.WriteSystem($"Started accepting messages from @{socket.RemoteEndPoint}");

    while (acceptingMessages)
    {
        try
        {
            Packet packet = ReceivePacket(socket);

            if (packet != null)
            {
                Log.WriteMessage(packet.ClientName, packet.Message);
                packetStack.Push(packet);
            }
        }
        catch (SocketException e)
        {
            RemoveClient(socket);
            break;
        }
    }
}

private static String ReceiveMessage(Socket socket)
{
    Byte[] expectedBytes = new Byte[ConnectionData.BUFFER_MAX_SIZE];
    Int32 receivedLength = socket.Receive(expectedBytes);

    Byte[] receivedBytes = new Byte[receivedLength];
    Buffer.BlockCopy(expectedBytes, 0, receivedBytes, 0, receivedLength);

    return Encoding.ASCII.GetString(receivedBytes);
}

private static void SendMessage(Socket socket, String message)
{
    try
    {
        socket.Send(Encoding.ASCII.GetBytes(message));
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

private static void RemoveClient(Socket socket)
{
    lock (removeClientLock)
    {
        var kvp0 = new KeyValuePair<String, Socket>(socket.RemoteEndPoint.ToString(), socket);

        if (clients.Contains(kvp0))
        {
            clients.TryRemove(socket.RemoteEndPoint.ToString(), out socket);
        }
        if (clientNamesIps.ContainsKey(socket.RemoteEndPoint.ToString()))
        {
            Log.WriteSystem($"Disconnected: {clientNamesIps[socket.RemoteEndPoint.ToString()]}");
            clientNamesIps.Remove(socket.RemoteEndPoint.ToString());
        }
    }
}