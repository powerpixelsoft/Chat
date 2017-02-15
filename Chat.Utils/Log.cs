using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Utils
{
    public class Log
    {
        public static void WriteSystem(String message)
        {
            Console.WriteLine($"> {message}");
        }
        public static void WriteMessage(String identity, String message)
        {
            String dateText = $"[{DateTime.Now:HH:mm}]";
            String identityText = $" @{identity}";

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("> ");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(dateText);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(identityText);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" \"{message}\"\n");

            Console.ResetColor();
        }
    }
}
