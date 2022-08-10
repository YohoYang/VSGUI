using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;

namespace VSGUI.API
{
    internal class DemuxApi
    {
        static string datarecevied = "";

        public static void StartDemux(string inputpath, string clitype, Action<string> DataReceivedCall, Action<string> ExitedCall)
        {
            if (inputpath != "" && File.Exists(inputpath))
            {
                datarecevied = "";
                if (clitype == "ffmpeg")
                {
                    ffmpegDemux(inputpath, DataReceivedCall, ExitedCall);
                }
                else if (clitype == "eac3to")
                {
                    eac3toDemux(inputpath, DataReceivedCall, ExitedCall);
                }
                else if (clitype == "mkvextract")
                {
                    //还没做，看情况
                }
                else
                {
                    //auto
                    string ext = Path.GetExtension(inputpath).ToLower();
                    if (ext == ".mp4" || ext == ".mov" || ext == ".flv")
                    {
                        ffmpegDemux(inputpath, DataReceivedCall, ExitedCall);
                    }
                    else
                    {
                        eac3toDemux(inputpath, DataReceivedCall, ExitedCall);
                    }
                }
            }
            else
            {
                MessageBoxApi.Show(LanguageApi.FindRes("fileNotExist"), LanguageApi.FindRes("error"));
                return;
            }

            static void eac3toDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
            {
                string common = "";
                string stra;
                string clipath = MainWindow.binpath + @"\tools\eac3to\";
                string ext = Path.GetExtension(inputpath);
                if (ext == ".thd+ac3")
                {
                    common = @"eac3to.exe" + " " + "\"" + inputpath + "\"" + " " + "\"" + inputpath + @".thd" + "\"" + " -log=nul" + " && " + @"eac3to.exe" + " " + "\"" + inputpath + "\"" + " " + "\"" + inputpath + @".ac3" + "\"" + " -log=nul";
                    stra = LanguageApi.FindRes("demuxThdAc3") + "...";
                }
                else
                {
                    common = @"eac3to.exe" + " " + "\"" + inputpath + "\"" + " " + "\"" + inputpath + @"_.*" + "\"" + " -log=nul";
                    stra = LanguageApi.FindRes("demuxing") + "...";
                }
                ProcessApi.RunProcess(clipath, common, DataReceived, Exited, out string pid);
                void DataReceived(DataReceivedEventArgs e, bool processIsExited)
                {
                    datarecevied += e.Data + "\n";
                    if (!string.IsNullOrEmpty(e.Data) && !processIsExited)
                    {
                        DataReceivedCall(stra);
                    }
                }
                void Exited()
                {
                    string timecount = "";
                    var x = Regex.Match(datarecevied, @"eac3to processing took (\d+) second");
                    if (x.Success)
                    {
                        timecount = x.Groups[1].ToString();
                        datarecevied = datarecevied.Replace("\x08", "").Replace(@"-------------------------------------------------------------------------------", "").Replace(@"---------", "").Replace("                                                                               ", "");
                        if (Regex.IsMatch(datarecevied, @"Applying.*delay"))
                        {
                            ExitedCall("eac3to" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish") + ", " + LanguageApi.FindRes("demuxAutoFixDelay") + ", " + LanguageApi.FindRes("timeConsuming") + " " + timecount + " " + LanguageApi.FindRes("second"));
                        }
                        else
                        {
                            ExitedCall("eac3to" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish") + ", " + LanguageApi.FindRes("timeConsuming") + " " + timecount + " " + LanguageApi.FindRes("second"));
                        }
                    }
                    else
                    {
                        ExitedCall("eac3to" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("error"));
                    }
                }
            }

            static void ffmpegDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
            {
                string common = "";
                //使用ffmpeg
                string clipath = MainWindow.binpath + @"\tools\ffmpeg\";
                string defaultcommon = @"ffmpeg.exe" + " -hide_banner -y ";
                string info = ProcessApi.RunSyncProcess(clipath, defaultcommon + "-i " + "\"" + inputpath + "\"");
                var x = Regex.Matches(info, @"Stream #(\d+:\d+).*?: (.*?): (.*?)\s");
                if (x.Count > 0)
                {
                    string commonParameter = "";
                    for (int i = 0; i < x.Count; i++)
                    {
                        if (x[i].Groups[2].Value == "Video" || x[i].Groups[2].Value == "Audio" || x[i].Groups[2].Value == "Subtitle")
                        {
                            commonParameter += " -map " + x[i].Groups[1].Value + " -c copy " + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + "." + x[i].Groups[3].Value + "\"";
                        }
                    }
                    string fullcommon = defaultcommon + "-i " + "\"" + inputpath + "\"" + commonParameter;
                    ProcessApi.RunProcess(clipath, fullcommon, DataReceived, Exited, out string pid);
                    void DataReceived(DataReceivedEventArgs e, bool processIsExited)
                    {
                        if (!string.IsNullOrEmpty(e.Data) && !processIsExited)
                        {
                            DataReceivedCall(LanguageApi.FindRes("demuxing"));
                        }
                    }
                    void Exited()
                    {
                        ExitedCall("ffmpeg" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish"));
                    }
                }
                else
                {
                    ExitedCall("ffmpeg" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("error"));
                }
            }
        }
    }
}
