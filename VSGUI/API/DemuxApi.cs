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
                    mkvextractDemux(inputpath, DataReceivedCall, ExitedCall);
                }
                else
                {
                    //auto
                    string ext = Path.GetExtension(inputpath).ToLower();
                    if (ext == ".mp4" || ext == ".mov" || ext == ".flv")
                    {
                        ffmpegDemux(inputpath, DataReceivedCall, ExitedCall);
                    }
                    else if (ext == ".mkv")
                    {
                        mkvextractDemux(inputpath, DataReceivedCall, ExitedCall);
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

            static void ffmpegDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
            {
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
                            commonParameter += " -map " + x[i].Groups[1].Value + " -c copy " + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + " - " + x[i].Groups[1].Value.Substring(x[i].Groups[1].Value.IndexOf(":") + 1) + "." + x[i].Groups[3].Value + "\"";
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

            static void eac3toDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
            {
                string common = "";
                string stra;
                string clipath = MainWindow.binpath + @"\tools\eac3to\";
                string ext = Path.GetExtension(inputpath);
                if (ext == ".thd+ac3")
                {
                    common = @"eac3to.exe" + " -demux " + "\"" + inputpath + "\"" + " " + "\"" + inputpath + @".thd" + "\"" + " -log=nul" + " && " + @"eac3to.exe" + " " + "\"" + inputpath + "\"" + " " + "\"" + inputpath + @".ac3" + "\"" + " -log=nul";
                    stra = LanguageApi.FindRes("demuxThdAc3") + "...";
                }
                else
                {
                    common = @"eac3to.exe" + " -demux " + "\"" + inputpath + "\"" + " " + "\"" + inputpath + @".*" + "\"" + " -log=nul";
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

            static void mkvextractDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
            {
                string clipath = MainWindow.binpath + @"\tools\mkvtoolnix\";
                string info = ProcessApi.RunSyncProcess(clipath, @"mkvinfo.exe" + " --ui-language en " + "\"" + inputpath + "\"", outputUTF8: true);
                var x = Regex.Matches(info, @"mkvextract: (\d*)(?:.|\s)*?Track type: (.*)(?:.|\s)*?Codec ID: (.*)");
                var xa = Regex.Matches(info, @"Attached(?:.|\s)*?File name: (.*)\s");

                if (x.Count > 0)
                {
                    string commonParameter = "";
                    for (int i = 0; i < x.Count; i++)
                    {
                        commonParameter += " " + x[i].Groups[1].Value + @":" + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + " - " + x[i].Groups[1].Value + "." + getOutputExt(x[i].Groups[3].Value) + "\"";
                    }
                    if (xa.Count > 0)
                    {
                        commonParameter += " attachments";
                        for (int i = 0; i < xa.Count; i++)
                        {
                            int aid = i + 1;
                            commonParameter += " " + aid + ":" + "\"" + Path.GetDirectoryName(inputpath) + @"\" + xa[i].Groups[1].Value + "\"";
                        }
                    }
                    string fullcommon = @"mkvextract.exe --ui-language en " + "\"" + inputpath + "\"" + @" tracks " + commonParameter;
                    ProcessApi.RunProcess(clipath, fullcommon, DataReceived, Exited, out string pid, outputUTF8: true);
                    void DataReceived(DataReceivedEventArgs e, bool processIsExited)
                    {
                        if (!string.IsNullOrEmpty(e.Data) && !processIsExited)
                        {
                            DataReceivedCall(LanguageApi.FindRes("demuxing"));
                        }
                    }
                    void Exited()
                    {
                        ExitedCall("mkvextract" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish"));
                    }
                }
                else
                {
                    ExitedCall("mkvextract" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("error"));
                }

                static string getOutputExt(string codec)
                {
                    Dictionary<string, string> CodecExt = new Dictionary<string, string>();
                    CodecExt.Add("A_AAC", "aac");
                    CodecExt.Add("A_AC3", "ac3");
                    CodecExt.Add("A_EAC3", "ac3");
                    CodecExt.Add("A_ALAC", "m4a");
                    CodecExt.Add("A_DTS", "dts");
                    CodecExt.Add("A_FLAC", "flac");
                    CodecExt.Add("A_MPEG/L2", "mp2");
                    CodecExt.Add("A_MPEG/L3", "mp3");
                    CodecExt.Add("A_OPUS", "ogg");
                    CodecExt.Add("A_VORBIS", "ogg");
                    CodecExt.Add("S_KATE", "ogg");
                    CodecExt.Add("V_THEORA", "ogg");
                    CodecExt.Add("A_PCM", "wav");
                    CodecExt.Add("A_REAL", "ram");
                    CodecExt.Add("A_TRUEHD", "thd");
                    CodecExt.Add("A_MLP", "thd");
                    CodecExt.Add("A_TTA1", "tta");
                    CodecExt.Add("A_WAVPACK4", "wv");
                    CodecExt.Add("S_HDMV/PGS", "sup");
                    CodecExt.Add("S_HDMV/TEXTST", "sub");
                    CodecExt.Add("S_TEXT/SSA", "ass");
                    CodecExt.Add("S_TEXT/ASS", "ass");
                    CodecExt.Add("S_SSA", "ass");
                    CodecExt.Add("S_ASS", "ass");
                    CodecExt.Add("S_TEXT/UTF8", "srt");
                    CodecExt.Add("S_TEXT/ASCII", "srt");
                    CodecExt.Add("S_VOBSUB", "idx");
                    CodecExt.Add("S_TEXT/USF", "usf");
                    CodecExt.Add("S_TEXT/WEBVTT", "vtt");
                    CodecExt.Add("V_MPEG1", "mpg");
                    CodecExt.Add("V_MPEG2", "mpg");
                    CodecExt.Add("ISO/AVC", "h264");
                    CodecExt.Add("ISO/HEVC", "h265");
                    CodecExt.Add("V_MS/VFW/FOURCC", "avi");
                    CodecExt.Add("V_REAL", "rm");
                    CodecExt.Add("V_VP8", "ivf");
                    CodecExt.Add("V_VP9", "ivf");

                    foreach (var item in CodecExt)
                    {
                        if (codec.Contains(item.Key))
                        {
                            return item.Value;
                        }
                    }
                    return "unknown";
                }
            }
        }
    }
}
