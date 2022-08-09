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

        public static void StartDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
        {
            if (inputpath != "" && File.Exists(inputpath))
            {
                datarecevied = "";
                string ext = Path.GetExtension(inputpath);
                string common = "";
                string clipath;
                //使用eac3to
                if (ext == ".ts")
                {
                    string stra;
                    clipath = MainWindow.binpath + @"\tools\eac3to\";
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
                        datarecevied += e.Data;
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
                            if (Regex.IsMatch(datarecevied, @"Applying.*delay"))
                            {
                                ExitedCall(LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish") + ", " + LanguageApi.FindRes("demuxAutoFixDelay") + ", " + LanguageApi.FindRes("timeConsuming") + " " + timecount + " " + LanguageApi.FindRes("second"));
                            }
                            else
                            {
                                ExitedCall(LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish") + ", " + LanguageApi.FindRes("timeConsuming") + " " + timecount + " " + LanguageApi.FindRes("second"));
                            }
                        }
                        else
                        {
                            ExitedCall(LanguageApi.FindRes("demux") + LanguageApi.FindRes("error"));
                        }
                    }
                    return;
                }

                //其余均使用ffmpeg
                clipath = MainWindow.binpath + @"\tools\ffmpeg\";
                string defaultcommon = @"ffmpeg.exe" + " -hide_banner -y ";
                string info = ProcessApi.RunSyncProcess(clipath, defaultcommon + "-i " + "\"" + inputpath + "\"");
                var x = Regex.Matches(info, @"Stream #(\d+:\d+).*?: (.*?): (.*?)\s");
                if (x.Count > 0)
                {
                    string commonParameter = "";
                    for (int i = 0; i < x.Count; i++)
                    {
                        if (x[i].Groups[2].Value == "Attachment")
                        {
                            //增加字体
                            var xa = Regex.Matches(info, @"Stream #" + x[i].Groups[1].Value + @": (.*?): (.*?)\s.*\s.*filename.*:\s(.*)\s");
                            if (xa.Count > 0)
                            {
                                commonParameter += " -map " + x[i].Groups[1].Value + " -c copy " + "\"" + Path.GetDirectoryName(inputpath) + @"\" + xa[i].Groups[3].Value + "\"";
                            }
                        }
                        else if (x[i].Groups[2].Value == "Data")
                        {
                            continue;
                        }
                        else
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
                        ExitedCall(LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish"));
                    }
                }
                else
                {
                    ExitedCall(LanguageApi.FindRes("demux") + LanguageApi.FindRes("error"));
                }
            }
            else
            {
                MessageBoxApi.Show(LanguageApi.FindRes("fileNotExist"), LanguageApi.FindRes("error"));
                return;
            }
        }
    }
}
