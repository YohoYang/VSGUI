using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;

namespace VSGUI.API
{
    internal class QueueApi
    {
        public static JsonArray queueListJson;
        public static int runningQueueCount = 0;
        public static long lastUpdateTime = 0;

        public static ObservableCollection<QueueMember> GetQueueMember()
        {
            ObservableCollection<QueueMember> queueItemData = new ObservableCollection<QueueMember>();
            var jsona = QueueApi.GetQueueList();
            queueItemData.Clear();
            for (int i = 0; i < jsona.Count; i++)
            {
                Visibility pVisibility = Visibility.Visible;
                if (int.Parse(jsona[i]["processvalue"].ToString()) == 0)
                {
                    pVisibility = Visibility.Collapsed;
                }
                string queueidstr = jsona[i]["queueid"].ToString();
                if (jsona[i]["group"].ToString() != "")
                {
                    queueidstr = QueueApi.GetGroupQueueidList(jsona[i]["group"].ToString())[0];
                }
                string fileinput = jsona[i]["input"].ToString();
                if (fileinput.Contains("|"))
                {
                    string tempstr = jsona[i]["input"].ToString();
                    string[] fileinputLists = tempstr.Split("|");
                    fileinput = "";
                    for (int j = 0; j < fileinputLists.Length; j++)
                    {
                        fileinput += Path.GetFileName(fileinputLists[j]);
                        if (j != fileinputLists.Length - 1)
                        {
                            fileinput += "|";
                        }
                    }
                }
                else
                {
                    fileinput = Path.GetFileName(fileinput);
                }
                queueItemData.Add(new QueueMember()
                {
                    Index = queueidstr,
                    Type = jsona[i]["type"].ToString(),
                    EncoderToolTip = EncoderApi.GetName(jsona[i]["type"].ToString(), int.Parse(jsona[i]["encoderid"].ToString())),
                    InputFilename = fileinput,
                    InputFilepath = jsona[i]["input"].ToString(),
                    OutputFilename = Path.GetFileName(jsona[i]["output"].ToString()),
                    OutputFilepath = jsona[i]["output"].ToString(),
                    ProgressValue = int.Parse(jsona[i]["processvalue"].ToString()),
                    ProgressVisibility = pVisibility,
                    StatusText = jsona[i]["statustext"].ToString(),
                });
            }
            return queueItemData;
        }

        public static string ProcessCommandStr(int queueid, string type, int encoderid, string[] input, string chapinput, string output, string scriptpath, out string tempoutput, out string clipath)
        {
            if (type == "mux")
            {
                tempoutput = Path.GetDirectoryName(output) + @"\" + "VSGUI_Job_" + queueid.ToString() + "_temp" + Path.GetExtension(output);
                string outputpath = tempoutput;
                string common = MuxApi.ProcessMuxCommandStr(input, chapinput, outputpath, queueid, out clipath);
                return common;
            }
            else
            {
                //处理pipe路径
                clipath = "";
                string pipecommon = "";
                string pipeconnect = "";
                //处理输入输出文件
                string inputpath = "\"" + input[0].Trim() + "\"";
                tempoutput = Path.GetDirectoryName(output) + @"\" + "VSGUI_Job_" + queueid.ToString() + "_temp" + Path.GetExtension(output);
                string outputpath = "\"" + tempoutput.Trim() + "\"";
                //根据类型处理参数
                if (type == "video")
                {
                    clipath = MainWindow.binpath + @"\vs\";
                    pipecommon = @"VSPipe.exe" + " -c y4m ";
                    pipeconnect = " - | ";
                }
                else if (type == "audio")
                {
                    clipath = MainWindow.binpath + @"\avs\";
                    pipecommon = @"avs2pipemod64.exe" + " -wav ";
                    pipeconnect = " | ";
                }
                //如果需要转换成script
                if (scriptpath != "")
                {
                    inputpath = "\"" + scriptpath.Trim() + "\"";
                }
                //处理encoder
                var encoderJson = JsonApi.ReadJsonObjectFromFile(MainWindow.binpath + @"\json\encoderprofiles.json");
                //全部依赖配置
                JsonObject thisJobj = encoderJson[type][encoderid].AsObject();
                string pEncoderName = thisJobj["encodername"].ToString();

                //自定义编码器兼容性处理
                string encoderpath;
                if (!thisJobj.ContainsKey("encoderpath"))
                {
                    encoderpath = "\"" + GetDefaultEncoderP(type, pEncoderName, "encoderpath") + "\"" + " ";
                }
                else
                {
                    encoderpath = "\"" + thisJobj["encoderpath"].ToString().Trim() + "\"" + " ";
                }
                string encoderpipeinputformat;
                if (!thisJobj.ContainsKey("pipeinputformat"))
                {
                    encoderpipeinputformat = GetDefaultEncoderP(type, pEncoderName, "pipeinputformat") + " ";
                }
                else
                {
                    encoderpipeinputformat = thisJobj["pipeinputformat"].ToString().Trim() + " ";
                }
                string encoderoutputformat;
                if (!thisJobj.ContainsKey("outputformat"))
                {
                    encoderoutputformat = GetDefaultEncoderP(type, pEncoderName, "outputformat") + " ";
                }
                else
                {
                    encoderoutputformat = thisJobj["outputformat"].ToString().Trim() + " ";
                }
                string encoderparameter = thisJobj["parameter"].ToString().Trim() + " ";
                string theCommandStr = pipecommon + inputpath + pipeconnect + encoderpath + encoderpipeinputformat + encoderparameter + encoderoutputformat + outputpath;
                return theCommandStr;
            }

        }

        private static string GetDefaultEncoderP(string encodertype, string encodername, string pname)
        {
            int index = 0;
            string returnStr = "";
            if (pname == "encoderpath")
            {
                index = 3;
            }
            else if (pname == "pipeinputformat")
            {
                index = 4;
            }
            else if (pname == "outputformat")
            {
                index = 5;
            }
            string[,] encoders = EncoderWindow.encoders;
            for (int i = 0; i < encoders.GetLength(0); i++)
            {
                if (encoders[i, 0].Equals(encodertype) && encoders[i, 1].Equals(encodername))
                {
                    returnStr = encoders[i, index];
                    break;
                }
            }
            if (pname == "encoderpath")
            {
                returnStr = System.IO.Directory.GetCurrentDirectory() + returnStr;
            }

            return returnStr;
        }

        public static void AddQueueList(string type, int encoderid, string[] input, string output, string chapinput = "", string group = "", string deletefile = "", string resolution = "", string subtitle = "", string audiocuttext = "", string audiofpstext = "", string audiodelaytext = "")
        {
            JsonArray queueJobj = GetQueueList();
            int newid;
            if (queueJobj.Count == 0)
            {
                newid = 1;
            }
            else
            {
                newid = int.Parse(queueJobj[queueJobj.Count - 1]["queueid"].ToString()) + 1;
            }
            string jobinput = input[0];
            if (input.Length > 1)
            {
                jobinput = "";
                for (int i = 0; i < input.Length; i++)
                {
                    jobinput += input[i];
                    if (i != input.Length - 1)
                    {
                        jobinput += "|";
                    }
                }
            }

            string jobchapinput = chapinput;
            if (chapinput != "")
            {
                ChapterApi chapter = new ChapterApi();
                if (!chapter.LoadOgm(chapinput))
                {
                    jobchapinput = CommonApi.GetAppTempPath() + "Job_" + newid + ".txt";
                }
            }

            JsonObject newqueue = new JsonObject();
            string tempoutputpath;
            newqueue.Add("queueid", newid);
            newqueue.Add("group", group);
            newqueue.Add("status", "waiting");
            newqueue.Add("type", type);
            newqueue.Add("encoderid", encoderid);
            newqueue.Add("input", jobinput);
            newqueue.Add("output", output);
            newqueue.Add("chapinput", chapinput);
            newqueue.Add("statustext", "");
            newqueue.Add("starttime", "");
            newqueue.Add("endtime", "");
            newqueue.Add("fps", "");
            newqueue.Add("command", ProcessCommandStr(newid, type, encoderid, input, jobchapinput, output, "", out tempoutputpath, out string clipathnew));
            newqueue.Add("clipath", clipathnew);
            newqueue.Add("deletefile", deletefile);
            newqueue.Add("processvalue", "0");
            newqueue.Add("processTheadId", "");
            newqueue.Add("totalFrames", "");
            newqueue.Add("script", "");
            newqueue.Add("scriptfilepath", "");
            newqueue.Add("outputtemp", tempoutputpath);
            newqueue.Add("resolution", resolution);
            newqueue.Add("subtitle", subtitle);
            newqueue.Add("audiocuttext", audiocuttext);
            newqueue.Add("audiofpstext", audiofpstext);
            newqueue.Add("audiodelaytext", audiodelaytext);

            queueJobj.Add(newqueue);

            queueListJson = queueJobj;
            SaveQueueList();
        }

        //处理实时进度
        public static void UpdateProgressStatus(string queueid, string message)
        {
            if (GetQueueListitem(queueid, "type").ToString() != "mux")
            {
                string encoderName = EncoderApi.GetEncoderName(GetQueueListitem(queueid, "type").ToString(), int.Parse(GetQueueListitem(queueid, "encoderid").ToString()));
                //x264 及 x265 及 nvenc 进度
                if (encoderName == "x264" || encoderName == "x265" || encoderName == "nvenc")
                {

                    var p = @"(\d+)[ ]{1,}frames:|(\d+.\d+)[ ]{1,}fps,";
                    var x = Regex.Matches(message, p);
                    if (x.Count >= 2)
                    {
                        int progressedFrames = int.Parse(x[0].Groups[1].ToString());
                        int totalFrames = int.Parse(GetQueueListitem(queueid, "totalFrames"));
                        float speed = float.Parse(x[1].Groups[2].ToString());
                        int remainSeconds = (int)((totalFrames - progressedFrames) / speed);

                        int pvalue = progressedFrames * 100 / totalFrames;
                        if (pvalue == 0) pvalue += 1;
                        SetQueueListitem(queueid, "processvalue", pvalue.ToString());
                        SetQueueListitem(queueid, "statustext", speed + "fps  -" + CommonApi.FormatSecondsToTimeStr(remainSeconds));
                    }
                }
                //qaac 及 flac进度
                else if (encoderName == "qaac" || encoderName == "flac")
                {
                    var p = @"(\d+.\d+)[ ]seconds[ ]\[(\d+)%\]";
                    var x = Regex.Matches(message, p);
                    if (x.Count >= 1)
                    {
                        int pvalue = int.Parse(x[0].Groups[2].ToString());
                        if (pvalue == 0) pvalue += 1;
                        SetQueueListitem(queueid, "processvalue", pvalue.ToString());
                        SetQueueListitem(queueid, "statustext", "+" + CommonApi.FormatSecondsToTimeStr((long)Math.Floor(double.Parse(x[0].Groups[1].ToString()))));
                    }
                    else
                    {
                        var x2 = Regex.Matches(message, @"Creating lwi index file (\d+)%");
                        if (x2.Count >= 1)
                        {
                            if (x2[0].Groups[1].ToString() != "100")
                            {
                                SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("queueCreatingLwi") + " " + x2[0].Groups[1] + "%");
                            }
                            else
                            {
                                SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("queueCreatingCache"));
                            }
                        }
                    }
                }
            }
            else
            {
                SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("queueMuxing"));
            }
            //显示进度到任务栏
            TaskbarApi.AutoSetProgressValueFromQueue();
        }

        /// <summary>
        /// 任务结束后调用
        /// </summary>
        /// <param name="queueid"></param>
        public static void DoWhenProcessFinish(string queueid)
        {
            bool isError = false;
            if (QueueApi.GetQueueListitem(queueid, "status") == "running")
            {
                string tempOutput = QueueApi.GetQueueListitem(queueid, "outputtemp");
                //改名
                if (tempOutput != "")
                {
                    isError = true;
                    if (File.Exists(tempOutput))
                    {
                        FileInfo file = new FileInfo(tempOutput);
                        if (file.Length != 0)
                        {
                            try
                            {
                                File.Move(QueueApi.GetQueueListitem(queueid, "outputtemp").Replace("\"", ""), QueueApi.GetQueueListitem(queueid, "output").Replace("\"", ""), true);
                                isError = false;
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    if (isError)
                    {
                        QueueApi.SetQueueListitem(queueid, "status", "error");
                        QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("error"));
                    }
                }
                //
                if (isError == false)
                {
                    QueueApi.SetQueueListitem(queueid, "status", "finish");
                    QueueApi.SetQueueListitem(queueid, "endtime", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString());
                    string timeConsuming = CommonApi.FormatSecondsToTimeStr(int.Parse(QueueApi.GetQueueListitem(queueid, "endtime")) - int.Parse(QueueApi.GetQueueListitem(queueid, "starttime")));
                    if (QueueApi.GetQueueListitem(queueid, "type") == "video")
                    {
                        if (QueueApi.GetQueueListitem(queueid, "totalFrames") != "")
                        {
                            var timeuse = long.Parse(QueueApi.GetQueueListitem(queueid, "endtime")) - long.Parse(QueueApi.GetQueueListitem(queueid, "starttime"));
                            if (timeuse == 0) timeuse = 1;
                            string finalSpeed = decimal.Round((decimal.Parse(QueueApi.GetQueueListitem(queueid, "totalFrames")) / (decimal)timeuse), 2).ToString();
                            QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("queueFinish") + " " + LanguageApi.FindRes("timeConsuming") + timeConsuming + " (" + finalSpeed + "fps)");
                        }
                        else
                        {
                            QueueApi.SetQueueListitem(queueid, "status", "error");
                            QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("error"));
                            isError = true;
                        }
                    }
                    else if (QueueApi.GetQueueListitem(queueid, "type") == "audio")
                    {
                        QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("queueFinish") + " " + LanguageApi.FindRes("timeConsuming") + timeConsuming);
                    }
                    else if (QueueApi.GetQueueListitem(queueid, "type") == "mux")
                    {
                        QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("queueFinish") + " " + LanguageApi.FindRes("timeConsuming") + timeConsuming);
                    }
                }
            }
            else if (QueueApi.GetQueueListitem(queueid, "status") == "stop")
            {
                QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("queueStoped"));
            }
            QueueApi.SetQueueListitem(queueid, "processvalue", "0");

            QueueApi.SetQueueListitem(queueid, "processTheadId", "");
            if (QueueApi.GetQueueListitem(queueid, "status") == "finish")
            {
                string[] deletefilelist = QueueApi.GetQueueListitem(queueid, "deletefile").Split(@"|");
                foreach (var deletefilelistitem in deletefilelist)
                {
                    if (File.Exists(deletefilelistitem))
                    {
                        try
                        {
                            File.Delete(deletefilelistitem);
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine("删除失败");
                        }
                    }
                }
            }
            //显示进度到任务栏
            TaskbarApi.AutoSetProgressValueFromQueue();
            QueueApi.SaveQueueList();
        }

        public static void UpdateTotalframes(string queueid)
        {
            if (GetQueueListitem(queueid, "type") == "video")
            {
                string vpypath;
                if (Path.GetExtension(GetQueueListitem(queueid, "input")) == ".vpy")
                {
                    vpypath = GetQueueListitem(queueid, "input");
                }
                else
                {
                    vpypath = GetQueueListitem(queueid, "scriptfilepath");
                }
                string result = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\vs\", "VSPipe.exe" + " --info " + "\"" + vpypath + "\"");
                if (result != null)
                {
                    var x = Regex.Matches(result, @"Frames: (\d+)|(\d+.\d+) fps");
                    if (x.Count >= 2)
                    {
                        SetQueueListitem(queueid, "totalFrames", x[0].Groups[1].Value);
                    }
                }
            }


        }

        /// <summary>
        /// 删除任务 v0.2.3
        /// </summary>
        /// <param name="queueid"></param>
        public static void DeleteQueueItem(string queueid)
        {
            ArrayList queueList = new ArrayList();
            ArrayList deleteList = new ArrayList();
            int delFileExistCount = 0;
            //获取列表
            if (GetQueueListitem(queueid, "group") != "")
            {
                var list = GetGroupQueueidList(QueueApi.GetQueueListitem(queueid, "group"));
                foreach (string item in list)
                {
                    queueList.Add(item);
                }
            }
            else
            {
                queueList.Add(queueid);
            }
            //获取deletelist
            foreach (string item in queueList)
            {
                string[] delList = GetQueueListitem(item, "deletefile").Split("|");
                foreach (string itemdel in delList)
                {
                    if (itemdel != "")
                    {
                        if (File.Exists(itemdel))
                        {
                            deleteList.Add(itemdel);
                            delFileExistCount += 1;
                        }
                    }
                }
                if (GetQueueListitem(item, "outputtemp") != "")
                {
                    if (File.Exists(GetQueueListitem(item, "outputtemp")))
                    {
                        deleteList.Add(GetQueueListitem(item, "outputtemp"));
                        delFileExistCount += 1;
                    }
                }
            }
            //判断是否有待删除文件存在
            if (delFileExistCount > 0)
            {
                string fileName = "";
                foreach (string item in deleteList)
                {
                    fileName += "\r\n" + Path.GetFileName(item);
                }
                var result = MessageBoxApi.Show(LanguageApi.FindRes("queueDeleteCacheTipsDesc") + fileName, buttontype: MessageWindow.MessageBoxButton.YesNoCancel);
                if (result == MessageWindow.MessageResult.Yes)
                {
                    foreach (string item in deleteList)
                    {
                        CommonApi.TryDeleteFile(item);
                    }
                }
                else if (result == MessageWindow.MessageResult.Cancel)
                {
                    return;
                }
            }
            //删除任务
            var queueJobj = GetQueueList();
            foreach (string item in queueList)
            {
                for (int i = 0; i < queueJobj.Count; i++)
                {
                    if (queueJobj[i]["queueid"].ToString() == item)
                    {
                        queueJobj.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public static void ResetQueueItem(string queueid)
        {
            var queueJobj = GetQueueList();
            for (int i = 0; i < queueJobj.Count; i++)
            {
                if (queueJobj[i]["queueid"].ToString() == queueid)
                {
                    queueJobj[i]["status"] = "waiting";
                    queueJobj[i]["statustext"] = "";
                    queueJobj[i]["starttime"] = "";
                    queueJobj[i]["endtime"] = "";
                    queueJobj[i]["fps"] = "";
                    queueJobj[i]["processvalue"] = "0";
                    queueJobj[i]["processTheadId"] = "";
                    return;
                }
            }
        }

        public static void SaveQueueList()
        {
            JsonApi.SaveJsonToFile(queueListJson, MainWindow.binpath + @"\json\queueList.json");
        }

        public static JsonArray GetQueueList()
        {
            if (queueListJson == null)
            {
                if (File.Exists(MainWindow.binpath + @"\json\queueList.json"))
                {
                    queueListJson = JsonApi.ReadJsonArrayFromFile(MainWindow.binpath + @"\json\queueList.json");
                }
                else
                {
                    queueListJson = new JsonArray();
                }
            }
            return queueListJson;
        }

        public static string GetQueueListitem(string queueid, string key)
        {
            var queueJobj = GetQueueList();
            foreach (var item in queueJobj)
            {
                if (item["queueid"].ToString() == queueid)
                {
                    return item[key].ToString();
                }
            }
            return null;
        }

        public static string GetQueueListitemFromSelectedIndex(int index, string key)
        {
            var queueJobj = GetQueueList();
            return queueJobj[index][key].ToString();
        }

        public static int FindQueueListitemIndexFromQueueid(string queueid)
        {
            var queueJobj = GetQueueList();
            int findindex = 0;
            for (int i = 0; i < queueJobj.Count; i++)
            {
                if (queueJobj[i]["queueid"].ToString() == queueid)
                {
                    findindex = i;
                    break;
                }
            }
            return findindex;
        }

        public static void SetQueueListitem(string queueid, string key, string value)
        {
            var queueJobj = GetQueueList();
            foreach (var item in queueJobj)
            {
                if (item["queueid"].ToString() == queueid)
                {
                    item[key] = value;
                    return;
                }
            }
        }

        /// <summary>
        /// 创建脚本文件
        /// </summary>
        /// <param name="queueid"></param>
        public static void MakeScriptFile(string queueid)
        {
            //判断是否需要复制必要文件
            bool needCopyFile = true;
            if (File.Exists(CommonApi.GetAppTempPath() + @"LSMASHSource.dll"))
            {
                if (UpdateApi.CalculateMD5(MainWindow.binpath + @"\vs\vapoursynth64\plugins\LSMASHSource.dll") == UpdateApi.CalculateMD5(CommonApi.GetAppTempPath() + @"LSMASHSource.dll"))
                {
                    needCopyFile = false;
                }
            }
            //复制文件
            if (needCopyFile)
            {
                try
                {
                    File.Copy(MainWindow.binpath + @"\vs\vapoursynth64\plugins\LSMASHSource.dll", CommonApi.GetAppTempPath() + @"LSMASHSource.dll");
                }
                catch (Exception)
                {

                }
            }
            //生成文件 v0.2.1
            if (GetQueueListitem(queueid, "type") == "video")
            {
                if (Path.GetExtension(GetQueueListitem(queueid, "input")) != ".vpy")
                {
                    string script = VideoApi.MakeVideoScript(GetQueueListitem(queueid, "input"), GetQueueListitem(queueid, "resolution"), GetQueueListitem(queueid, "subtitle"));
                    string scriptpath = CommonApi.GetAppTempPath() + "Job_" + queueid + ".vpy";
                    string command = ProcessCommandStr(int.Parse(queueid), "video", int.Parse(GetQueueListitem(queueid, "encoderid")), new string[] { GetQueueListitem(queueid, "input") }, "", GetQueueListitem(queueid, "output"), scriptpath, out string temp1, out string temp2);
                    SetQueueListitem(queueid, "script", script);
                    SetQueueListitem(queueid, "scriptfilepath", scriptpath);
                    if (!GetQueueListitem(queueid, "deletefile").Contains(scriptpath)) SetQueueListitem(queueid, "deletefile", GetQueueListitem(queueid, "deletefile") + "|" + scriptpath);
                    SetQueueListitem(queueid, "command", command);
                }
            }
            else if (GetQueueListitem(queueid, "type") == "audio")
            {
                if (Path.GetExtension(GetQueueListitem(queueid, "input")) != ".avs")
                {
                    string script = AudioApi.MakeAudioScript(int.Parse(GetQueueListitem(queueid, "encoderid")), GetQueueListitem(queueid, "audiocuttext"), GetQueueListitem(queueid, "audiofpstext"), GetQueueListitem(queueid, "input"), GetQueueListitem(queueid, "audiodelaytext"));
                    string scriptpath = CommonApi.GetAppTempPath() + "Job_" + queueid + ".avs";
                    string command = ProcessCommandStr(int.Parse(queueid), "audio", int.Parse(GetQueueListitem(queueid, "encoderid")), new string[] { GetQueueListitem(queueid, "input") }, "", GetQueueListitem(queueid, "output"), scriptpath, out string temp1, out string temp2);
                    SetQueueListitem(queueid, "script", script);
                    SetQueueListitem(queueid, "scriptfilepath", scriptpath);
                    if (!GetQueueListitem(queueid, "deletefile").Contains(scriptpath)) SetQueueListitem(queueid, "deletefile", GetQueueListitem(queueid, "deletefile") + "|" + scriptpath);
                    SetQueueListitem(queueid, "command", command);
                }
            }
            else if (GetQueueListitem(queueid, "type") == "mux")
            {
                string chapterTempPath = CommonApi.GetAppTempPath() + "Job_" + queueid + ".txt";
                ChapterApi chapter = new ChapterApi();
                string chapinputStr = GetQueueListitem(queueid, "chapinput");
                if (chapinputStr != null)
                {
                    if (!chapter.LoadOgm(chapinputStr))
                    {
                        if (chapter.LoadFile(chapinputStr))
                        {
                            chapter.SaveText(chapterTempPath);
                            SetQueueListitem(queueid, "deletefile", GetQueueListitem(queueid, "deletefile") + "|" + chapterTempPath);
                        }
                    }
                }
            }

            //写入script文件
            if (GetQueueListitem(queueid, "scriptfilepath") != "")
            {
                File.WriteAllText(GetQueueListitem(queueid, "scriptfilepath"), GetQueueListitem(queueid, "script"));
            }
        }

        /// <summary>
        /// 特殊格式处理 
        /// </summary>
        /// <param name="queueid"></param>
        public static void SpecialFormatPreProcess(string queueid)
        {
            //flv格式需要先抽取视频流（仅处理简易压制）
            if (GetQueueListitem(queueid, "type") == "video")
            {
                if (Path.GetExtension(GetQueueListitem(queueid, "input")) == ".flv")
                {
                    string inputpath = GetQueueListitem(queueid, "input");
                    string outputpath = "";
                    string clipath = MainWindow.binpath + @"\encoder\ffmpeg\";
                    string defaultcommon = @"ffmpeg.exe" + " -hide_banner -y ";
                    string info = ProcessApi.RunSyncProcess(clipath, defaultcommon + "-i " + "\"" + inputpath + "\"");
                    var x = Regex.Matches(info, @"Stream #(\d+:\d+).*?: (.*?): (.*?) ");
                    if (x.Count > 0)
                    {
                        string commonParameter = "";
                        for (int i = 0; i < x.Count; i++)
                        {
                            if (x[i].Groups[2].Value.ToLower() == "video")
                            {
                                outputpath = Path.GetDirectoryName(GetQueueListitem(queueid, "output")) + @"\" + Path.GetFileNameWithoutExtension(inputpath) + "." + x[i].Groups[3].Value;
                                commonParameter += " -map " + x[i].Groups[1].Value + " -c copy " + "\"" + outputpath + "\"";
                            }
                        }
                        string fullcommon = defaultcommon + "-i " + inputpath + commonParameter + " & exit";
                        string resultstr = ProcessApi.RunSyncProcess(clipath, fullcommon);
                        SetQueueListitem(queueid, "input", outputpath);
                        SetQueueListitem(queueid, "deletefile", GetQueueListitem(queueid, "deletefile") + "|" + outputpath);
                    }
                }
            }
        }

        /// <summary>
        /// 视频vpy输入检测
        /// </summary>
        /// <param name="videoinputboxText"></param>
        /// <param name="cuttextboxText"></param>
        /// <param name="fpstextboxText"></param>
        /// <param name="cutischeckedIsChecked"></param>
        /// <param name="isError"></param>
        public static void VpyFileInputCheck(string videoinputboxText, out string cuttextboxText, out string fpstextboxText, out bool cutischeckedIsChecked, out bool isError)
        {
            cuttextboxText = "";
            fpstextboxText = "";
            cutischeckedIsChecked = false;
            isError = false;

            string inputpath = videoinputboxText;
            if (!string.IsNullOrEmpty(inputpath))
            {
                string inputsuffix = Path.GetExtension(inputpath).ToLower();
                if (inputsuffix == ".vpy")
                {
                    string result = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\vs\", @"VSPipe.exe" + " --info " + "\"" + inputpath + "\"");
                    if (result != null)
                    {
                        var x = Regex.Matches(result, @"Frames: (\d+)|(\d+.\d+) fps");
                        if (x.Count >= 2)
                        {
                            //获取fps
                            fpstextboxText = x[1].Groups[2].Value;
                            //cut检测
                            string autoGenCutConfig = IniApi.IniReadValue("AutoGenerateCut");
                            if (autoGenCutConfig == "") autoGenCutConfig = "true";
                            if (bool.Parse(autoGenCutConfig))
                            {
                                //获取vpy脚本及每行内容
                                string vpyfilestr = File.ReadAllText(inputpath);
                                string[] vpyfilestrlist = vpyfilestr.Replace("\r\n", "\n").Split("\n");
                                //获取最终输出
                                string finaloutVar = "";
                                var xo0 = Regex.Matches(vpyfilestr, @"(.*?)\.set_output");
                                if (xo0.Count > 0)
                                {
                                    finaloutVar = xo0[0].Groups[1].Value;
                                }
                                //创建键值对
                                Dictionary<string, string> cutmap = new Dictionary<string, string>();
                                //定义最终str
                                string finalcutstr = "";
                                //错误标记
                                bool isCutError = false;
                                //遍历每一行
                                foreach (var listitem in vpyfilestrlist)
                                {
                                    //判断非注释
                                    if (!listitem.TrimStart().StartsWith("#"))
                                    {
                                        //获取被赋值变量
                                        string assigned = "";
                                        var x0 = Regex.Matches(listitem, @"(.*?)\s*?=");
                                        if (x0.Count > 0)
                                        {
                                            if (x0[0].Groups.Count > 0)
                                            {
                                                assigned = x0[0].Groups[1].Value;
                                            }
                                        }
                                        //获取裁剪参数
                                        var x1 = Regex.Matches(listitem, @".*?\.std\.Trim\((.*?),(.*?)\)|([0-9a-zA-Z]*?)\[(\d+(?:\:|\,|\s)*\d+)\]");
                                        string linecutstr = "";
                                        if (x1.Count > 0)
                                        {
                                            for (int i = 0; i < x1.Count; i++)
                                            {
                                                string assign = x1[i].Groups[1].Value;
                                                if (assign == "") assign = x1[i].Groups[3].Value;
                                                if (assigned == assign) // ?最简单的保护，如果是复合变量直接放弃
                                                {
                                                    string cutmessagestr = x1[i].Groups[2].Value;
                                                    if (cutmessagestr == "") cutmessagestr = x1[i].Groups[4].Value;
                                                    string[] cutmessagelist = cutmessagestr.Replace(":", ",").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Split(",");//获得cut的前后帧
                                                    linecutstr += "[" + cutmessagelist[0] + ":" + cutmessagelist[1] + "]";
                                                    if (i != x1.Count - 1)
                                                    {
                                                        linecutstr += "+";
                                                    }
                                                }
                                                else
                                                {
                                                    isCutError = true;
                                                }
                                            }
                                            //处理变量map
                                            if (cutmap.ContainsKey(assigned))
                                            {
                                                cutmap[assigned] = cutmap[assigned] + @"&" + linecutstr;
                                            }
                                            else
                                            {
                                                cutmap.Add(assigned, linecutstr);
                                            }
                                        }
                                    }
                                }
                                if (!isCutError && cutmap.Count > 0)
                                {
                                    finalcutstr = cutmap[finaloutVar];
                                }
                                cuttextboxText = finalcutstr;
                                if (finalcutstr != "")
                                {
                                    cutischeckedIsChecked = true;
                                }
                            }
                        }
                        else
                        {
                            var x2 = Regex.Match(result, @"Python exception.*");
                            MessageBoxApi.Show(x2.Value, LanguageApi.FindRes("error"));
                            isError = true;
                        }
                    }
                }
                else
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("advancedEncodeInputErrorTipsDesc"), LanguageApi.FindRes("error"));
                    isError = true;
                }
            }
            else
            {
                isError = true;
            }
        }

        /// <summary>
        /// 简易压制输入检测
        /// </summary>
        /// <param name="videoinputboxText"></param>
        /// <param name="audioinputboxtext"></param>
        /// <param name="isError"></param>
        public static void SimpleEncodeFileInputCheck(string videoinputboxText, out string videoinputboxTextnew, out string audioinputboxtext)
        {
            audioinputboxtext = "";
            videoinputboxTextnew = "";

            string inputpath = videoinputboxText;
            if (!string.IsNullOrEmpty(inputpath))
            {
                string result = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\encoder\ffmpeg\", @"ffmpeg.exe" + " -hide_banner -y -i " + "\"" + inputpath + "\"");
                var videoinfo = Regex.Matches(result, @"Stream.*Video:.*");
                var audioinfo = Regex.Matches(result, @"Stream.*Audio:.*");
                if (videoinfo.Count >= 1)
                {
                    if (!videoinfo[0].ToString().Contains("attached pic"))
                    {
                        videoinputboxTextnew = videoinputboxText;
                    }
                }
                if (audioinfo.Count >= 1)
                {
                    audioinputboxtext = videoinputboxText;
                }
            }
        }


        /// <summary>
        /// 音频输入检测
        /// </summary>
        /// <param name="audioinputboxText"></param>
        /// <param name="audiodelayboxText"></param>
        /// <param name="isError"></param>
        public static void AudioFileInputCheck(string audioinputboxText, out string audiodelayboxText, out bool isError)
        {
            audiodelayboxText = "0";
            isError = false;
            string inputpath = audioinputboxText;
            if (!string.IsNullOrEmpty(inputpath))
            {
                string inputsuffix = Path.GetExtension(inputpath).ToLower();
                //if (inputsuffix == ".ts" || inputsuffix == ".m2ts" || inputsuffix == ".mkv" || inputsuffix == ".mp4")


                string result = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\tools\mediainfo\", @"MediaInfo.exe" + " " + "\"" + inputpath + "\"");
                if (result != null)
                {
                    if (result.Contains("Audio"))
                    {
                        var x = Regex.Matches(result, @"Audio(?: #)?(\d*)(?s:.)*?Delay relative to video.*?: (.*?)ms");
                        if (x.Count >= 1)
                        {
                            //就取第一个
                            string message = x[0].Groups[2].Value.Trim();
                            audiodelayboxText = message;
                        }
                    }
                    else
                    {
                        isError = true;
                    }
                   
                }

                ////eac3to检测延迟
                //string result = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\tools\eac3to\", @"eac3to.exe" + " " + "\"" + inputpath + "\"");
                //if (result != null)
                //{
                //    var x = Regex.Matches(result, @"\d+: (AAC|AC3|WAV|AC3|DTS|THD|FLAC).*");
                //    if (x.Count >= 1)
                //    {
                //        //就取第一个
                //        string message = x[0].ToString();
                //        var x2 = Regex.Matches(message, @", (-?\d+)ms");
                //        if (x2.Count > 0)
                //        {
                //            audiodelayboxText = x2[0].Groups[1].Value;
                //        }
                //    }
                //}




                if (audiodelayboxText == "0")
                {
                    //尝试从文件名获取延迟
                    var x = Regex.Matches(inputpath, @"(-?\d+)ms");
                    if (x.Count >= 1)
                    {
                        //就取第一个
                        string message = x[0].Groups[1].Value.Trim();
                        audiodelayboxText = message;
                    }
                }
            }
        }

        /// <summary>
        /// 简易音频压制输入检测
        /// </summary>
        /// <param name="videoinputboxText"></param>
        /// <param name="audioinputboxtext"></param>
        /// <param name="isError"></param>
        public static void SimpleEncodeAudioFileInputCheck(string audioinputboxText, out bool isError)
        {
            isError = true;

            string inputpath = audioinputboxText;
            if (!string.IsNullOrEmpty(inputpath))
            {
                string result = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\encoder\ffmpeg\", @"ffmpeg.exe" + " -hide_banner -y -i " + "\"" + inputpath + "\"");
                var audioinfo = Regex.Matches(result, @"Stream.*Audio:.*");
                if (audioinfo.Count >= 1)
                {
                    isError = false;
                }
            }
        }

        /// <summary>
        /// 通过queue的组名称获取该组的ID
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public static string[] GetGroupQueueidList(string groupname)
        {
            var queueJobj = GetQueueList();
            ArrayList list = new ArrayList();
            foreach (var item in queueJobj)
            {
                if (item["group"].ToString() == groupname)
                {
                    list.Add(item["queueid"].ToString());
                }
            }
            return (string[])list.ToArray(typeof(string));
        }

        /// <summary>
        /// 判断组mux是否准备完成
        /// </summary>
        /// <param name="queueid"></param>
        /// <returns></returns>
        public static bool CheckGroupMuxJobIsReady(string queueid)
        {
            bool result = true;
            string[] list = GetGroupQueueidList(GetQueueListitem(queueid, "group"));
            foreach (var qid in list)
            {
                if (GetQueueListitem(qid, "type") != "mux")
                {
                    if (GetQueueListitem(qid, "status") != "finish")
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 判断组是否可以删除
        /// </summary>
        /// <param name="queueid"></param>
        /// <returns></returns>
        public static bool CheckGroupJobCanDelete(string queueid)
        {
            bool result = true;
            string[] list = GetGroupQueueidList(GetQueueListitem(queueid, "group"));
            foreach (var qid in list)
            {
                if (GetQueueListitem(qid, "status") == "running" || GetQueueListitem(qid, "status") == "pause")
                {
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取左下角的队列信息文本
        /// </summary>
        /// <returns></returns>
        public static string GetQueueInfoText()
        {
            var queueJobj = GetQueueList();
            int finishCount = 0;
            int runningCount = 0;
            string progress = "";

            for (int i = 0; i < queueJobj.Count; i++)
            {
                if (queueJobj[i]["status"].ToString() == "finish")
                {
                    finishCount += 1;
                }
                else if (queueJobj[i]["status"].ToString() == "running")
                {
                    runningCount += 1;
                    progress += queueJobj[i]["processvalue"] + @"% ";
                }
            }
            string infostr = LanguageApi.FindRes("queue") + ":" + finishCount + @"/" + queueJobj.Count + "  ";
            if (progress != "")
            {
                infostr += LanguageApi.FindRes("queueRuning") + ":" + runningCount + " - " + progress;
            }
            return infostr;
        }

    }


    public class QueueMember
    {
        public string? Index { get; set; }
        public string? Type { get; set; }
        public string? EncoderToolTip { get; set; }
        public string? InputFilename { get; set; }
        public string? InputFilepath { get; set; }
        public string? OutputFilename { get; set; }
        public string? OutputFilepath { get; set; }
        public int? ProgressValue { get; set; }
        public Visibility? ProgressVisibility { get; set; }
        public string? StatusText { get; set; }
    }
}
