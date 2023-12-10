using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;

namespace VSGUI.API
{

    class UpdateApi
    {
        private static JsonObject? updatejson;

        public static void UpdateCheck(string proxy, Action<string> PCall, Action Finsihed)
        {
            try
            {
                new Thread(
                        async () =>
                        {
                            PCall(LanguageApi.FindRes("updateChecking"));
                            UpdateCore();
                            //增加一个update其他文件的方法
                            UpdateLocal7z(PCall);
                            PCall(LanguageApi.FindRes("updateCheckingLocalVersion"));
                            CheckLocalVersionJson();
                            PCall(LanguageApi.FindRes("updateGetingServerVersion"));
                            await GetUpdateVersionAsync(proxy);
                            if (updatejson != null && File.Exists(MainWindow.binpath + @"\json\version.json"))
                            {
                                PCall(LanguageApi.FindRes("updateFilesChecking"));
                                string[] updatelist = CheckVersionByMD5();
                                //7z包
                                if (!(File.Exists(MainWindow.binpath + @"\7z.exe") && File.Exists(MainWindow.binpath + @"\7z.dll")))
                                {
                                    await DownloadFile(@"https://cloud.sbsub.com/vsgui/7z.exe", MainWindow.binpath + @"\7z.exe", proxy);
                                    await DownloadFile(@"https://cloud.sbsub.com/vsgui/7z.dll", MainWindow.binpath + @"\7z.dll", proxy);
                                }
                                if (!(File.Exists(MainWindow.binpath + @"\vs\7z.exe") && File.Exists(MainWindow.binpath + @"\vs\7z.dll")))
                                {
                                    Directory.CreateDirectory(MainWindow.binpath + @"\vs\");
                                    File.Copy(MainWindow.binpath + @"\7z.exe", MainWindow.binpath + @"\vs\7z.exe", true);
                                    File.Copy(MainWindow.binpath + @"\7z.dll", MainWindow.binpath + @"\vs\7z.dll", true);
                                }
                                if (IniApi.IniReadValue("AutoUpdate") == "false")
                                {
                                    if (updatelist.Length > 0)
                                    {
                                        PCall(LanguageApi.FindRes("updateButDisable"));
                                        return;
                                    }
                                }
#if DEBUG
                                PCall("debug mode, disable auto update.");
                                Finsihed();
                                return;
#endif
                                //下载开始
                                for (int i = 0; i < updatelist.Length; i++)
                                {
                                    PCall(LanguageApi.FindRes("updateDownloading") + (i + 1) + @"/" + updatelist.Length);
                                    await DownloadUpdateFile(updatelist[i], proxy);
                                }
                                PCall(LanguageApi.FindRes("p033"));

                            }
                            else
                            {
                                PCall(LanguageApi.FindRes("updateCheckFail"));
                            }
                            Finsihed();
                        }
                    ).Start();
            }
            catch (Exception)
            {

            }

            //}
            //下载模块列表json
            //逐一判断模块的本地版本与最新版本（并支持设置模块是否开启），如果相同不更新模块，不同则弹出更新提示窗，下载该模块最新压缩包，检查下载文件无误后解压
            //更新编码器配置（如果有设置）
        }

        private static void UpdateLocal7z(Action<string> PCall)
        {
            //更新开始
            if (QueueApi.runningQueueCount > 0)
            {
                PCall(LanguageApi.FindRes("updateButJobIsRuning"));
            }
            else
            {
                string ds = ".update.7z";
                var jsonlocal = JsonApi.ReadJsonObjectFromFile(MainWindow.binpath + @"\json\version.json");
                string[] update7zFileLists = CommonApi.GetAllFileInFolder(MainWindow.binpath + @"\update\");
                for (int i = 0; i < update7zFileLists.Length; i++)
                {
                    if (update7zFileLists[i].Substring(1) == "VSGUI.exe" + ds)
                    {
                        continue;
                    }
                    PCall(LanguageApi.FindRes("updateUpdating") + (i + 1) + @"/" + update7zFileLists.Length);
                    string filepath = update7zFileLists[i];
                    string rpath = filepath.Replace(MainWindow.binpath + @"\update", "").Replace(ds, "");
                    if (File.Exists(filepath))
                    {
                        string unzippath = Directory.GetCurrentDirectory() + rpath;
                        ProcessApi.RunSyncProcess(MainWindow.binpath, @"7z.exe -y x " + @"""" + filepath + @"""" + @" -o" + @"""" + Path.GetDirectoryName(unzippath) + @"""");
                        File.Delete(filepath);
                        if (Directory.GetFiles(Path.GetDirectoryName(filepath)).Length == 0 && Directory.GetDirectories(Path.GetDirectoryName(filepath)).Length == 0)
                        {
                            Directory.Delete(Path.GetDirectoryName(filepath));
                        }
                        if (jsonlocal.ContainsKey(rpath))
                        {
                            jsonlocal[rpath]["f"] = CalculateMD5(unzippath);
                        }
                        else
                        {
                            JsonObject jsonsubdata = new JsonObject();
                            jsonsubdata.Add("f", CalculateMD5(unzippath));
                            jsonlocal.Add(rpath, jsonsubdata);
                        }
                        JsonApi.SaveJsonToFile(jsonlocal, MainWindow.binpath + @"\json\version.json");
                    }
                }
                PCall(LanguageApi.FindRes("updateIsNewest"));
            }
            try
            {
                CommonApi.DeleteEmptyDirectories(MainWindow.binpath + @"\update\");
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 主程序更新
        /// </summary>
        private static void UpdateCore()
        {
            System.Diagnostics.Process[] pro = System.Diagnostics.Process.GetProcesses();//获取已开启的所有进程
            //遍历所有查找到的进程
            for (int i = 0; i < pro.Length; i++)
            {
                //判断此进程是否是要查找的进程
                if (pro[i].ProcessName.ToString() == "vsguiupdater")
                {
                    pro[i].Kill();//结束进程
                }
            }

            string coreupdatepath = MainWindow.binpath + @"\update\\VSGUI.exe.update.7z";
            if (File.Exists(coreupdatepath))
            {
                string updaterpath = MainWindow.binpath + @"\vsguiupdater.exe";
                Process.Start(updaterpath);
            }
        }

        /// <summary>
        /// 检查本地版本json
        /// </summary>
        private static void CheckLocalVersionJson()
        {
            string versionpath = MainWindow.binpath + @"\json\version.json";
            if (!File.Exists(versionpath))
            {
                string[] filelist = CommonApi.GetAllFileInFolder(Directory.GetCurrentDirectory());
                JsonObject jsondata = new JsonObject();
                foreach (string file in filelist)
                {
                    if (Path.GetFileName(file) == "config.ini" || Path.GetFileName(file) == "queueList.json" || Path.GetFileName(file) == "encoderprofiles.json" || Path.GetFileName(file) == "version.json")
                    {
                        continue;
                    }
                    JsonObject jsonsubdata = new JsonObject
                    {
                        { "f", CalculateMD5(file) }
                    };
                    jsondata.Add(file.Replace(Directory.GetCurrentDirectory(), ""), jsonsubdata);
                }
                JsonApi.SaveJsonToFile(jsondata, MainWindow.binpath + @"\json\version.json");
            }
            else
            {
                var jsondata = JsonApi.ReadJsonObjectFromFile(versionpath);
                string localcoremd5 = CalculateMD5(Directory.GetCurrentDirectory() + @"\VSGUI.exe");
                if (jsondata.ContainsKey(@"\VSGUI.exe"))
                {
                    jsondata[@"\VSGUI.exe"]["f"] = localcoremd5;
                }
                else
                {
                    JsonObject jsonsubdata = new JsonObject
                    {
                        { "f", localcoremd5 }
                    };
                    jsondata.Add(@"\VSGUI.exe", jsonsubdata);
                }
                JsonApi.SaveJsonToFile(jsondata, MainWindow.binpath + @"\json\version.json");
            }
        }

        /// <summary>
        /// 获取服务器文件信息
        /// </summary>
        private static async Task GetUpdateVersionAsync(string proxy)
        {
            try
            {
                if (proxy != "")
                {
                    string proxyURL = proxy;
                    WebProxy webProxy = new WebProxy(proxyURL);

                    HttpClientHandler httpClientHandler = new HttpClientHandler
                    {
                        Proxy = webProxy
                    };
                    using (var httpclient = new HttpClient(httpClientHandler))
                    {
                        string updateBaseUrl = @"https://cloud.sbsub.com/vsgui/";
                        var jsonstr = await httpclient.GetStringAsync(updateBaseUrl + "update.json");
                        var json = JsonApi.ReadJsonFromString(jsonstr);
                        updatejson = json;
                    }
                }
                else
                {
                    using (var httpclient = new HttpClient())
                    {
                        string updateBaseUrl = @"https://cloud.sbsub.com/vsgui/";
                        var jsonstr = await httpclient.GetStringAsync(updateBaseUrl + "update.json");
                        var json = JsonApi.ReadJsonFromString(jsonstr);
                        updatejson = json;
                    }
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("NetError on GetUpdateVersionAsync");
            }
        }

        private static async Task DownloadUpdateFile(string rurl, string proxy)
        {
            string ziprpath = rurl + @".update.7z";
            string filepath = MainWindow.binpath + @"\update\" + ziprpath.Substring(1);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            JsonObject jsonserver = updatejson;
            string zipmd5 = jsonserver[rurl]["z"].ToString();
            if (File.Exists(filepath))
            {
                if (CalculateMD5(filepath) == zipmd5)
                {
                    return;
                }
                else
                {
                    File.Delete(filepath);
                }
            }
            string updateBaseUrl = @"https://cloud.sbsub.com/vsgui/";
            await DownloadFile(updateBaseUrl + ziprpath.Replace(@"\", @"/").Substring(1), filepath, proxy);
        }

        public static async Task DownloadFile(string url, string localpath, string proxy)
        {
            try
            {
                if (proxy != "")
                {
                    string proxyURL = proxy;
                    WebProxy webProxy = new WebProxy(proxyURL);

                    HttpClientHandler httpClientHandler = new HttpClientHandler
                    {
                        Proxy = webProxy
                    };
                    using (var httpclient = new HttpClient(httpClientHandler))
                    {
                        var response = await httpclient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                var fileInfo = new FileInfo(localpath);
                                using (var fileStream = fileInfo.OpenWrite())
                                {
                                    await stream.CopyToAsync(fileStream);
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (var httpclient = new HttpClient())
                    {
                        var response = await httpclient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                var fileInfo = new FileInfo(localpath);
                                using (var fileStream = fileInfo.OpenWrite())
                                {
                                    await stream.CopyToAsync(fileStream);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("NetError on DownloadFile");
            }
        }

        private static string[] CheckVersionByMD5()
        {
            ArrayList updatelist = new ArrayList();
            var jsonlocal = JsonApi.ReadJsonObjectFromFile(MainWindow.binpath + @"\json\version.json");
            JsonObject jsonserver = updatejson;
            foreach (var item in jsonserver)
            {
                if (jsonlocal.ContainsKey(item.Key))
                {
                    if (item.Value["f"].ToString() != jsonlocal[item.Key]["f"].ToString())
                    {
                        updatelist.Add(item.Key);
                    }
                }
                else
                {
                    if (File.Exists(Directory.GetCurrentDirectory() + item.Key))
                    {
                        string localmd5 = CalculateMD5(Directory.GetCurrentDirectory() + item.Key);
                        if (item.Value["f"].ToString() == localmd5)
                        {
                            JsonObject jsonsubdata = new JsonObject
                            {
                                { "f", localmd5 }
                            };
                            jsonlocal.Add(item.Key, jsonsubdata);
                            JsonApi.SaveJsonToFile(jsonlocal, MainWindow.binpath + @"\json\version.json");
                        }

                        else
                        {
                            updatelist.Add(item.Key);
                        }
                    }
                    else
                    {
                        updatelist.Add(item.Key);
                    }
                }
            }
            return (string[])updatelist.ToArray(typeof(string));
        }



        public static string CalculateMD5(string filepath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }



        public static async void UpdateEncoderProfiles(string url, Action Callback, Action CallError, string proxy)
        {
            JsonObject localencoderjson;
            JsonObject serverencoderjson;
            string jsonpath = MainWindow.binpath + @"\json\encoderprofiles.json";
            if (File.Exists(jsonpath))
            {
                localencoderjson = JsonApi.ReadJsonObjectFromFile(jsonpath);
            }
            else
            {
                localencoderjson = JsonApi.ReadJsonFromString(@"{""video"":[{""name"":""Default"",""encodername"":""x264"",""parameter"":""--crf 22.5"",""suffix"":"".h264"",""tag"":""custom""}],""audio"":[{""name"":""Default"",""encodername"":""qaac"",""parameter"":""--tvbr 73"",""suffix"":"".aac"",""normalize"":true,""tag"":""custom""}]}");
            }
            try
            {
                if (IniApi.IniReadValue("UseNetEncoderJson") == "true")
                {
                    if (proxy != "")
                    {
                        string proxyURL = proxy;
                        WebProxy webProxy = new WebProxy(proxyURL);

                        HttpClientHandler httpClientHandler = new HttpClientHandler
                        {
                            Proxy = webProxy
                        };
                        using (var httpclient = new HttpClient(httpClientHandler))
                        {
                            var jsonstr = await httpclient.GetStringAsync(url);
                            serverencoderjson = JsonApi.ReadJsonFromString(jsonstr);
                        }
                    }
                    else
                    {
                        using (var httpclient = new HttpClient())
                        {
                            var jsonstr = await httpclient.GetStringAsync(url);
                            serverencoderjson = JsonApi.ReadJsonFromString(jsonstr);
                        }
                    }
                    JsonApi.SaveJsonToFile(UpdateSubProfiles(localencoderjson, serverencoderjson), jsonpath);
                }
                else
                {
                    int customcount = 0;
                    foreach (JsonObject item in localencoderjson["video"].AsArray())
                    {
                        if (item["tag"].ToString() == "custom")
                        {
                            customcount++;
                        }
                    }
                    if (customcount == 0)
                    {
                        localencoderjson["video"].AsArray().Add(JsonApi.ReadJsonFromString(@"{""name"":""Default"",""encodername"":""x264"",""parameter"":""--crf 22.5"",""suffix"":"".h264"",""tag"":""custom""}"));
                    }
                    customcount = 0;
                    foreach (JsonObject item in localencoderjson["audio"].AsArray())
                    {
                        if (item["tag"].ToString() == "custom")
                        {
                            customcount++;
                        }
                    }
                    if (customcount == 0)
                    {
                        localencoderjson["audio"].AsArray().Add(JsonApi.ReadJsonFromString(@"{""name"":""Default"",""encodername"":""qaac"",""parameter"":""--tvbr 73"",""suffix"":"".aac"",""normalize"":true,""tag"":""custom""}"));
                    }
                    JsonApi.SaveJsonToFile(UpdateSubProfiles(localencoderjson, JsonApi.ReadJsonFromString(@"{""video"":[],""audio"":[]}")), jsonpath);
                }
                EncoderApi.PurgeEncoderJsonCache();
                Callback();
            }
            catch (Exception)
            {
                CallError();
            }

            static JsonObject UpdateSubProfiles(JsonObject localencoderjson, JsonObject serverencoderjson)
            {
                var newjson = JsonApi.ReadJsonFromString(@"{""video"":[],""audio"":[]}");

                DoUpdate("video");
                DoUpdate("audio");

                return newjson;

                void DoUpdate(string type)
                {
                    if (serverencoderjson.ContainsKey(type))
                    {
                        //添加server的
                        foreach (JsonObject jsonitem in serverencoderjson[type] as JsonArray)
                        {
                            if (jsonitem.ContainsKey("tag"))
                            {
                                jsonitem["tag"] = "server";
                            }
                            else
                            {
                                jsonitem.Add("tag", "server");
                            }
                            (newjson[type] as JsonArray).Add(JsonApi.ReadJsonFromString(jsonitem.ToString()));
                        }
                        //添加原来的custom的
                        foreach (JsonObject jsonitem in localencoderjson[type] as JsonArray)
                        {
                            if (jsonitem.ContainsKey("tag"))
                            {
                                if (jsonitem["tag"].ToString() == "custom")
                                {
                                    (newjson[type] as JsonArray).Add(JsonApi.ReadJsonFromString(jsonitem.ToString()));
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
