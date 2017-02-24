using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Utils
{
    using System.IO;

    public class Packet
    {
        public String Ip { get; set; }
        public String ClientName { get; set; }
        public String Message { get; set; }
        public Byte[] DataBytes { get; set; }
        public String Flag { get; set; } = "null";

        public Byte[] Encode()
        {
            List<Byte> data = new List<Byte>();

            Byte[] ipBytes = Encoding.UTF8.GetBytes(Ip);
            Byte[] nameBytes = Encoding.UTF8.GetBytes(ClientName);
            Byte[] messageBytes = Encoding.UTF8.GetBytes(Message);
            Byte[] flagBytes = Encoding.UTF8.GetBytes(Flag);

            Byte[] ipBytes_length = BitConverter.GetBytes(ipBytes.Length);
            Byte[] ipBytes_pos = BitConverter.GetBytes(32);

            data.AddRange(ipBytes_length);
            data.AddRange(ipBytes_pos);             

            Byte[] nameBytes_length = BitConverter.GetBytes(nameBytes.Length);
            Byte[] nameBytes_pos = BitConverter.GetBytes(32 + ipBytes.Length);

            data.AddRange(nameBytes_length);
            data.AddRange(nameBytes_pos);

            Byte[] messagBytes_length = BitConverter.GetBytes(messageBytes.Length);
            Byte[] messagBytes_pos = BitConverter.GetBytes(32 + ipBytes.Length + nameBytes.Length);

            data.AddRange(messagBytes_length);
            data.AddRange(messagBytes_pos);

            Byte[] flagBytes_length = BitConverter.GetBytes(flagBytes.Length);
            Byte[] flagBytes_pos = BitConverter.GetBytes(32 + ipBytes.Length + nameBytes.Length + messageBytes.Length);

            data.AddRange(flagBytes_length);
            data.AddRange(flagBytes_pos);

            data.AddRange(ipBytes);
            data.AddRange(nameBytes);
            data.AddRange(messageBytes);
            data.AddRange(flagBytes);

            DataBytes = data.ToArray();
            return data.ToArray();
        }
        public static Packet Decode(Byte[] dataBytes)
        {
            Packet packet = new Packet();

            packet.Ip = Encoding.UTF8.GetString(ExtractData(dataBytes, 0, 4));
            packet.ClientName = Encoding.UTF8.GetString(ExtractData(dataBytes, 8, 12));
            packet.Message = Encoding.UTF8.GetString(ExtractData(dataBytes, 16, 20));
            packet.Flag = Encoding.UTF8.GetString(ExtractData(dataBytes, 24, 28));
            packet.DataBytes = dataBytes;

            return packet;
        }
        private static Byte[] ExtractData(byte[] source, int offsetLength, int offsetPosition)
        {
            Byte[] infoBytes = new byte[4];
            Buffer.BlockCopy(source, offsetLength, infoBytes, 0, 4);
            Int32 length = BitConverter.ToInt32(infoBytes, 0);

            infoBytes = new byte[4];
            Buffer.BlockCopy(source, offsetPosition, infoBytes, 0, 4);
            Int32 offset = BitConverter.ToInt32(infoBytes, 0);

            Byte[] valueBytes = new Byte[length];
            Buffer.BlockCopy(source, offset, valueBytes, 0, length);

            return valueBytes;
        }
    }
}
