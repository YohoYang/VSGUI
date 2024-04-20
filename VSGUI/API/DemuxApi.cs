using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
                try
                {
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
                catch (Exception)
                {
                    ExitedCall(LanguageApi.FindRes("p003"));
                }
            }
            else
            {
                MessageBoxApi.Show(LanguageApi.FindRes("fileNotExist"), LanguageApi.FindRes("error"));
                ExitedCall("");
                return;
            }

            static void ffmpegDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
            {
                string clipath = MainWindow.binpath + @"\encoder\ffmpeg\";
                string defaultcommon = @"ffmpeg.exe" + " -hide_banner -y ";
                string info = ProcessApi.RunSyncProcess(clipath, defaultcommon + "-i " + "\"" + inputpath + "\"");
                var x = Regex.Matches(info, @"Stream #(\d+:\d+).*?: (.*?): (.*?)[\s|,]");
                if (x.Count > 0)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\");
                    DataReceivedCall(LanguageApi.FindRes("demuxing"));
                    string commonParameter = "";
                    for (int i = 0; i < x.Count; i++)
                    {
                        if (x[i].Groups[2].Value == "Video" || x[i].Groups[2].Value == "Audio" || x[i].Groups[2].Value == "Subtitle")
                        {
                            string format = x[i].Groups[3].Value;
                            if (format == "subrip")
                            {
                                format = "srt";
                            }
                            commonParameter += " -map " + x[i].Groups[1].Value + " -c copy " + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + " - " + x[i].Groups[1].Value.Substring(x[i].Groups[1].Value.IndexOf(":") + 1) + "." + format + "\"";
                        }
                        if (commonParameter.Length > 6000)
                        {
                            string fullcommonfast = defaultcommon + "-i " + "\"" + inputpath + "\"" + commonParameter;
                            ProcessApi.RunProcess(clipath, fullcommonfast, DataReceived, Exited1, Pided);
                            commonParameter = "";
                        }
                    }
                    string fullcommon = defaultcommon + "-i " + "\"" + inputpath + "\"" + commonParameter;
                    ProcessApi.RunProcess(clipath, fullcommon, DataReceived, Exited, Pided);
                    void DataReceived(string data, string qid, bool processIsExited)
                    {
                        //if (!string.IsNullOrEmpty(data) && !processIsExited)
                        //{
                        //    DataReceivedCall(LanguageApi.FindRes("demuxing"));
                        //}
                    }
                    void Exited()
                    {
                        ExitedCall("ffmpeg" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish"));
                    }
                    void Exited1()
                    {
                    }
                    void Pided(string pid)
                    {
                        //QueueApi.SetQueueListitem(queueid, "processTheadId", pid);
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
                string info = ProcessApi.RunSyncProcess(clipath, @"eac3to.exe" + " " + "\"" + inputpath + "\"" + " -log=nul"); // 提取正则(.*?)(?: #)?(\d*)\nID((?s:.)*?)\n\n
                var x = Regex.Matches(info, @"(\d+): (.*?)[, ] (.*)\s");
                if (x.Count > 0)
                {
                    string commonParameter = "";
                    for (int i = 0; i < x.Count; i++)
                    {
                        string ext = x[i].Groups[2].Value.ToLower();
                        string spext = "";
                        //进行格式的设置
                        if (x[i].Groups[1].Value == "1")
                        {
                            //视频流
                            if (ext.Contains(@"/"))
                            {
                                spext = ext.Substring(0, ext.IndexOf(@"/"));
                            }
                            else
                            {
                                spext = ext;
                            }
                        }
                        else
                        {
                            if (ext.Contains(@"pcm"))
                            {
                                spext = "wav";
                            }
                            else if (ext.Contains(@"(pgs)"))
                            {
                                spext = "sup";
                            }
                            else if (ext.Contains(@"(ass)"))
                            {
                                spext = "ass";
                            }
                            else if (ext.Contains(@"(srt)"))
                            {
                                spext = "srt";
                            }
                            else
                            {
                                if (ext.Contains(@"/"))
                                {
                                    spext = ext.Substring(ext.IndexOf(@"/") + 1);
                                }
                                else
                                {
                                    spext = ext;
                                }
                            }
                        }

                        //处理结束
                        string outFileinfoText = x[i].Groups[3].Value.Trim().Replace(@"/", "_").Replace(@"\", "_").Replace(@":", "_").Replace(@"*", "_").Replace(@"?", "_").Replace(@"""", "_").Replace(@"<", "_").Replace(@">", "_").Replace(@"|", "_").Replace(@" ", "").Replace(@",", "_");
                        string outFileName = Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileName(inputpath) + "_[T" + x[i].Groups[1].Value + "]_" + outFileinfoText + "." + spext;
                        //删除文件名中的延迟信息
                        outFileName = Regex.Replace(outFileName, @"-?\d+ms", string.Empty);
                        //Unknown audio track跳过处理
                        if (x[i].Groups[2].Value == "Unknown audio track")
                        {
                            continue;
                        }
                        //特殊thd+ac3处理
                        if (x[i].Groups[2].Value == "TrueHD/AC3")
                        {
                            string thdOutFileName = outFileName.Replace(spext, "thd");
                            commonParameter += " " + x[i].Groups[1].Value + ":" + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\" + Path.GetFileName(thdOutFileName) + "\"";
                        }
                        //正常添加字符串
                        commonParameter += " " + x[i].Groups[1].Value + ":" + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\" + Path.GetFileName(outFileName) + "\"";
                    }
                    common = @"eac3to.exe" + " " + "\"" + inputpath + "\"" + commonParameter + " -progressnumbers -log=nul";
                }
                else
                {
                    ExitedCall("eac3to" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("error"));
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\");
                ProcessApi.RunProcess(clipath, common, DataReceived, Exited, Pided);
                void DataReceived(string data, string qid, bool processIsExited)
                {
                    datarecevied += data + "\n";
                    if (data != null && data.StartsWith("process:"))
                    {
                        stra = LanguageApi.FindRes("demuxing") + " " + data.Replace("process: ", "");
                    }
                    else
                    {
                        stra = LanguageApi.FindRes("demuxing") + "...";
                    }
                    if (!string.IsNullOrEmpty(data) && !processIsExited)
                    {
                        DataReceivedCall(stra);
                    }
                }
                void Exited()
                {
                    string timecount = "";
                    var x = Regex.Match(datarecevied, @"eac3to processing tooks? (.*)");
                    if (x.Captures.Count > 0)
                    {
                        timecount = x.Groups[1].ToString().Trim();
                        datarecevied = datarecevied.Replace("\x08", "").Replace(@"-------------------------------------------------------------------------------", "").Replace(@"---------", "").Replace("                                                                               ", "");
                        if (Regex.IsMatch(datarecevied, @"Applying.*delay"))
                        {
                            ExitedCall("eac3to" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish") + ", " + LanguageApi.FindRes("demuxAutoFixDelay") + ", " + LanguageApi.FindRes("timeConsuming") + " " + timecount);
                        }
                        else
                        {
                            ExitedCall("eac3to" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish") + ", " + LanguageApi.FindRes("timeConsuming") + " " + timecount);
                        }
                    }
                    else
                    {
                        ExitedCall("eac3to" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("error"));
                    }
                }
                void Pided(string pid)
                {
                    //QueueApi.SetQueueListitem(queueid, "processTheadId", pid);
                }
            }

            static void mkvextractDemux(string inputpath, Action<string> DataReceivedCall, Action<string> ExitedCall)
            {
                TimeSpan timeout = new TimeSpan(0, 0, 1);
                string clipath = MainWindow.binpath + @"\tools\mkvtoolnix\";
                string info = ProcessApi.RunSyncProcess(clipath, @"mkvinfo.exe" + " --ui-language en " + "\"" + inputpath + "\"");
                //新的mkv信息读取方法
                string[] infolines = info.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int infoindex = 0;
                ArrayList mkvid = new ArrayList();
                ArrayList typeid = new ArrayList();
                ArrayList codecid = new ArrayList();
                for (int i = 0; i < infolines.Length; i++)
                {
                    string cLine = infolines[i];
                    if (cLine.Contains("mkvextract:"))
                    {
                        var result = Regex.Match(cLine, @"mkvextract: (\d*)");
                        mkvid.Add(result.Groups[1].Value);
                    }
                    if (cLine.Contains("Track type:"))
                    {
                        var result = Regex.Match(cLine, @"Track type: (.*)");
                        typeid.Add(result.Groups[1].Value);
                    }
                    if (cLine.Contains("Codec ID:"))
                    {
                        var result = Regex.Match(cLine, @"Codec ID: (.*)");
                        codecid.Add(result.Groups[1].Value);
                    }
                    if (mkvid.Count > infoindex && typeid.Count > infoindex && codecid.Count > infoindex)
                    {
                        infoindex++;
                    }
                }
                //老的字体读取方法（能用就不用改了吧）
                var xa = Regex.Matches(info, @"Attached(?:.|\s)*?File name: (.*)\s", RegexOptions.Multiline, timeout);

                if (infoindex > 0)
                {
                    string commonParameter = "";
                    Directory.CreateDirectory(Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\");
                    DataReceivedCall(LanguageApi.FindRes("demuxing"));
                    //先解压附件
                    if (xa.Count > 0)
                    {
                        commonParameter = " attachments";
                        for (int i = 0; i < xa.Count; i++)
                        {
                            int aid = i + 1;
                            commonParameter += " " + aid + ":" + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\" + xa[i].Groups[1].Value + "\"";
                            if (commonParameter.Length > 6000)
                            {
                                string fullcommonfast = @"mkvextract.exe --ui-language en " + "\"" + inputpath + "\"" + @" tracks " + commonParameter;
                                ProcessApi.RunProcess(clipath, fullcommonfast, DataReceived, Exited1, Pided);
                                commonParameter = " attachments";
                            }
                        }
                        string fullcommon1 = @"mkvextract.exe --ui-language en " + "\"" + inputpath + "\"" + @" tracks " + commonParameter;
                        ProcessApi.RunProcess(clipath, fullcommon1, DataReceived, Exited1, Pided);
                    }
                    //然后解压正常内容
                    commonParameter = "";
                    for (int i = 0; i < infoindex; i++)
                    {
                        commonParameter += " " + mkvid[i].ToString() + @":" + "\"" + Path.GetDirectoryName(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + " - " + mkvid[i].ToString() + "." + getOutputExt(codecid[i].ToString()) + "\"";
                        if (commonParameter.Length > 6000)
                        {
                            string fullcommonfast = @"mkvextract.exe --ui-language en " + "\"" + inputpath + "\"" + @" tracks " + commonParameter;
                            ProcessApi.RunProcess(clipath, fullcommonfast, DataReceived, Exited1, Pided);
                            commonParameter = "";
                        }
                    }
                    string fullcommon = @"mkvextract.exe --ui-language en " + "\"" + inputpath + "\"" + @" tracks " + commonParameter;
                    ProcessApi.RunProcess(clipath, fullcommon, DataReceived, Exited, Pided);
                    void DataReceived(string data, string qid, bool processIsExited)
                    {
                        //if (!string.IsNullOrEmpty(data) && !processIsExited)
                        //{
                        //    DataReceivedCall(LanguageApi.FindRes("demuxing"));
                        //}
                    }
                    void Exited()
                    {
                        ExitedCall("mkvextract" + LanguageApi.FindRes("demux") + LanguageApi.FindRes("finish"));
                    }
                    void Exited1()
                    {

                    }
                    void Pided(string pid)
                    {
                        //QueueApi.SetQueueListitem(queueid, "processTheadId", pid);
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
