using System;
using System.IO;
using System.Text;

namespace HK.Logger
{
    public static class Log
    {
        private static void log(string str, string title)
        {
            using (StreamWriter sw = new StreamWriter("table.log", true, Encoding.UTF8))
            {
                string logstr = String.Format("{2}[{0:G}]：{1}", DateTime.Now, str, title);
                sw.WriteLineAsync(logstr);
            }
        }

        public static void LogMsg(string str)
        {
            log(str, "");
            Console.WriteLine(str);
        }

        public static void LogWarn(string str)
        {
            log(str, "warn");
        }

        public static void logError(string str)
        {
            log(str, "error");
            throw new Exception(str);
        }
    }
}
