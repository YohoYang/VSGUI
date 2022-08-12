using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VSGUI.API
{
    internal class LogApi
    {
        public static void WriteLog(string logstr)
        {
            string isEnableLog = IniApi.IniReadValue("EnableLog");
            if (isEnableLog == "true")
            {
                string logpath = Directory.GetCurrentDirectory() + @"\log";
                Directory.CreateDirectory(logpath);
                string logFileName = DateTime.Now.ToString("yyyyMMdd") + ".txt";
                File.AppendAllText(logpath + @"\" + logFileName, logstr);
            }
        }
    }
}
