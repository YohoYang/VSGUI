using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VSGUI.API
{
    internal class LogApi
    {
        public static void WriteLog(string logstr)
        {
            try
            {
                string isEnableLog = IniApi.IniReadValue("EnableLog");
                if (isEnableLog == "true")
                {
                    string logpath = Directory.GetCurrentDirectory() + @"\log";
                    Directory.CreateDirectory(logpath);
                    string logFileName = "run_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
                    File.AppendAllText(logpath + @"\" + logFileName, logstr);
                }
            }
            catch (Exception)
            {

            }
        }

        public static void WriteCrashLog(string logstr)
        {
            string logpath = Directory.GetCurrentDirectory() + @"\log";
            Directory.CreateDirectory(logpath);
            string logFileName = "crash_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";
            File.WriteAllText(logpath + @"\" + logFileName, logstr);
        }
    }
}
