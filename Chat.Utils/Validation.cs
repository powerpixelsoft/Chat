namespace Chat.Utils
{
    using System;
    using System.Text.RegularExpressions;

    public class Validation
    {
        public static Boolean GetIp(String raw, out String result)
        {
            Regex ipRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            var matches = ipRegex.Matches(raw);

            if (matches.Count == 0)
            {
                result = String.Empty;
                return false;
            }

            result =matches[0].ToString();
            return true;
        }

        public static Boolean GetPort(String portText, out Int32 result)
        {
            Regex portRegex = new Regex(@"\b\d{4,5}\b");
            var matches = portRegex.Matches(portText);

            if (matches.Count == 0)
            {
                result = 0;
                return false;
            }

            result = Convert.ToInt32(matches[0].ToString());
            return true;
        }
    }
}