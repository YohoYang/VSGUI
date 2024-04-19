using System;
using System.Collections;
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
        public static string ProcessMuxCommandStr(string[] input, string subinput, string chapinput, string output, int queueid, out string clipath)
        {
            if (Path.GetExtension(output.Replace("\"", "")).ToLower() == ".mp4")
            {
                //ffmpeg mode
                //clipath = MainWindow.binpath + @"\tools\ffmpeg\";
                //string addstr = "";
                //foreach (var item in input)
                //{
                //    if (item != "")
                //    {
                //        string tempitempath = item;
                //        if (Path.GetExtension(item) == ".txt" && ChapterApi.ChapterFormatCheck(item))
                //        {
                //            string ffmeta = ChapterApi.MakeFFmpegMetaData(item);
                //            string chapterpath = Path.GetTempPath() + @"vsgui\" + "Job_" + queueid.ToString() + ".txt";
                //            Directory.CreateDirectory(Path.GetTempPath() + @"vsgui\");
                //            File.WriteAllText(chapterpath, ffmeta);
                //            tempitempath = chapterpath;
                //        }
                //        addstr += " -i " + "\"" + tempitempath + "\"";
                //    }
                //}
                //string theCommandStr = "ffmpeg.exe -hide_banner -y " + addstr + " -c copy " + "\"" + output + "\"";
                //return theCommandStr;
                //mp4box mode
                clipath = MainWindow.binpath + @"\tools\mp4box\";
                string addstr = "";
                foreach (var item in input)
                {
                    if (item != "")
                    {
                        addstr += " -add " + "\"" + item + "\"";
                    }
                }
                //拆分章节输入
                if (chapinput != "")
                {
                    addstr += " -add " + "\"" + chapinput + ":chap" + "\"";
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
                        addstr += " " + "\"" + item + "\"";
                    }
                }
                if (subinput != "")
                {
                    addstr += @" --language 0:und --default-track-flag 0:no ^""^(^"" ^""" + subinput + @"^"" ^""^)^"" ";
                }
                if (chapinput != "")
                {
                    addstr += " --chapters" + " " + "\"" + chapinput + "\"";
                }
                string theCommandStr = @"mkvmerge.exe --ui-language en" + addstr + " -o " + "\"" + output + "\"";
                return theCommandStr;
            }
            clipath = "";
            return "";
        }


        public static void StartSMux(string[] input, string subinput, string chapinput, string outputsuffix, Action<string> DataReceivedCall, Action<string> ExitedCall)
        {
            string datarecevied = "";
            string output = Path.GetDirectoryName(input[0]) + @"\" + Path.GetFileNameWithoutExtension(input[0]) + @"_mux." + outputsuffix.ToLower();
            CommonApi.TryDeleteFile(output);

            string chapterTempPath = chapinput;
            ChapterApi chapter = new ChapterApi();
            if (chapinput != "")
            {
                if (!chapter.LoadOgm(chapinput))
                {
                    if (chapter.LoadFile(chapinput))
                    {
                        chapterTempPath = CommonApi.GetAppTempPath() + "Job_smux.txt";
                        chapter.SaveText(chapterTempPath);
                    }
                }
            }
            string common = ProcessMuxCommandStr(input, subinput, chapterTempPath, output, -1, out string clipath);

            ProcessApi.RunProcess(clipath, common, DataReceived, Exited, Pided);
            void DataReceived(string data, bool processIsExited)
            {
                datarecevied += data;
                if (!string.IsNullOrEmpty(data) && !processIsExited)
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
                CommonApi.TryDeleteFile(CommonApi.GetAppTempPath() + "Job_-1" + ".txt");
                CommonApi.TryDeleteFile(CommonApi.GetAppTempPath() + "Job_smux.txt");
            }
            void Pided(string pid)
            {
                //QueueApi.SetQueueListitem(queueid, "processTheadId", pid);
            }
        }
    }
}
