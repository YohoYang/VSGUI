using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VSGUI.API
{
    internal class MuxApi
    {
        public static string ProcessMuxCommandStr(string[] input, string output, out string clipath)
        {
            if (Path.GetExtension(output.Replace("\"", "")).ToLower() == ".mp4")
            {
                clipath = MainWindow.binpath + @"\tools\mp4box\";
                string addstr = "";
                foreach (var item in input)
                {
                    if (item != "")
                    {
                        string additionstr = "";
                        if (Path.GetExtension(item) == ".txt")
                        {
                            additionstr = ":chap";
                        }
                        addstr += " -add " + "\"" + item + additionstr + "\"";
                    }
                }
                string theCommandStr = "MP4Box.exe" + addstr + " -new " + "\"" + output + "\"";
                return theCommandStr;
            }
            else if (Path.GetExtension(output.Replace("\"", "")).ToLower() == ".mkv")
            {
                clipath = MainWindow.binpath + @"\tools\mkvtoolnix\";
                string addstr = "";
                foreach (var item in input)
                {
                    if (item != "")
                    {
                        if (Path.GetExtension(item) == ".txt")
                        {
                            addstr += " --chapters";
                        }
                        addstr += " " + "\"" + item + "\"";
                    }
                }
                string theCommandStr = @"mkvmerge.exe" + addstr + " -o " + "\"" + output + "\"";
                return theCommandStr;
            }
            clipath = "";
            return "";
        }


        static string datarecevied = "";
        public static void StartSMux(string[] input, string outputsuffix, Action<string> DataReceivedCall, Action<string> ExitedCall)
        {
            string output = Path.GetDirectoryName(input[0]) + @"\" + Path.GetFileNameWithoutExtension(input[0]) + @"_mux." + outputsuffix.ToLower();
            CommonApi.TryDeleteFile(output);

            string common = ProcessMuxCommandStr(input, output, out string clipath);

            ProcessApi.RunProcess(clipath, common, DataReceived, Exited, out string pid, true);
            void DataReceived(DataReceivedEventArgs e, bool processIsExited)
            {
                datarecevied += e.Data;
                if (!string.IsNullOrEmpty(e.Data) && !processIsExited)
                {
                    DataReceivedCall(LanguageApi.FindRes("muxMuxing"));
                }
            }
            void Exited()
            {
                if (!Regex.IsMatch(datarecevied, @"Error|错误|Filters not connected"))
                {
                    ExitedCall(LanguageApi.FindRes("mux") + LanguageApi.FindRes("finish"));
                }
                else
                {
                    ExitedCall(LanguageApi.FindRes("mux") + LanguageApi.FindRes("error"));
                }
            }

        }
    }
}
