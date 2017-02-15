private static void SendMessage(Stream clientStream, String message)
{
    try
    {
        Byte[] messageBytes = Encoding.ASCII.GetBytes(message);
        clientStream.Write(messageBytes, 0, messageBytes.Length);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

private static void ReceiveMessage(Stream clientStream)
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

            Log.WriteMessage("Server", Encoding.ASCII.GetString(receivedBytes));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}