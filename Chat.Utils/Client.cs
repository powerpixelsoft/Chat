using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Utils
{
    using System.Net.Sockets;

    public class Client
    {
        public String Name { get; set; }
        public Socket Socket { get; set; }

        public Client(String name, Socket socket)
        {
            Name = name;
            Socket = socket;
        }

        public Client()
        {
            
        }
    }
}
