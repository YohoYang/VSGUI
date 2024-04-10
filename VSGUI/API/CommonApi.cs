using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace VSGUI.API
{
    internal class CommonApi
    {
        /// <summary>
        /// 将UNIX时间转换为标准时间
        /// </summary>
        /// <param name="unix"></param>
        /// <returns></returns>
        public static DateTime ConvertUNIXSecondToDate(long unix)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan toNow = new TimeSpan(unix * 10000000);
            DateTime dtResult = dtStart.Add(toNow);
            return dtResult;
        }

        /// <summary>
        /// 转换文件大小方法
        /// </summary>
        /// <param name="size">字节值</param>
        /// <returns></returns>
        public static string HumanReadableFilesize(double bytes)
        {
            int unit = 1024;
            if (bytes < unit) return bytes + " B";
            int exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return String.Format("{0:F1} {1}B", bytes / Math.Pow(unit, exp), "KMGTPE"[exp - 1]).ToLower();
        }

        /// <summary>
        /// 秒转可读
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public static string FormatSecondsToTimeStr(long l)
        {
            string str = "";
            int hour = 0;
            int minute = 0;
            int second = 0;
            second = (int)l;
            if (second > 60)
            {
                minute = second / 60;
                second = second % 60;
            }
            if (minute > 60)
            {
                hour = minute / 60;
                minute = minute % 60;
            }
            str = hour + ":" + minute.ToString().PadLeft(2, '0') + ":" + second.ToString().PadLeft(2, '0');
            return str;
        }

        //生成随机数
        public static string GetNewSeed()
        {
            string chars = "0123456789abcdefghijklmnpqrstuvwxyz";
            Random randrom = new Random();

            string str = "";

            for (int i = 0; i < 8; i++)
            {
                str += chars[randrom.Next(chars.Length)];//randrom.Next(int i)返回一个小于所指定最大值的非负随机数
            }
            return str;
        }

        /// <summary>
        /// 获取目录下文件
        /// </summary>
        /// <param name = "path" > 指定目录 </ param >
        /// < returns ></ returns >
        public static string[] GetFileOnlyInFolder(string path)
        {
            ArrayList array = new ArrayList();
            DirectoryInfo d = new DirectoryInfo(path);
            FileSystemInfo[] fsinfos = d.GetFileSystemInfos();
            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if (!(fsinfo is DirectoryInfo))
                {
                    array.Add(fsinfo.FullName);
                }
            }
            return (string[])array.ToArray(typeof(string));
        }

        /// <summary>
        /// 获取目录下所有文件
        /// </summary>
        /// <param name = "path" > 指定目录 </ param >
        /// < returns ></ returns >
        public static string[] GetAllFileInFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                return new string[] { };
            }
            ArrayList array = new ArrayList();
            DirectoryInfo d = new DirectoryInfo(path);
            FileSystemInfo[] fsinfos = d.GetFileSystemInfos();
            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if (fsinfo is DirectoryInfo)
                {
                    string[] _temp = GetAllFileInFolder(fsinfo.FullName);
                    foreach (var item in _temp)
                    {
                        array.Add(item);
                    }
                }
                else
                {
                    array.Add(fsinfo.FullName);
                }
            }
            return (string[])array.ToArray(typeof(string));
        }

        /// <summary>
        /// 尝试删除文件
        /// </summary>
        /// <param name="path"></param>
        public static void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 获取网络日期时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetNetDateTime()
        {
            WebRequest request = null;
            WebResponse response = null;
            WebHeaderCollection headerCollection = null;
            string datetime = string.Empty;
            try
            {
                request = WebRequest.Create("https://www.aliyun.com");
                request.Timeout = 3000;
                request.Credentials = CredentialCache.DefaultCredentials;
                response = (WebResponse)request.GetResponse();
                headerCollection = response.Headers;
                foreach (var h in headerCollection.AllKeys)
                { if (h == "Date") { datetime = headerCollection[h]; } }
                if (datetime != null)
                {
                    return Convert.ToDateTime(datetime);
                }
                else
                {
                    return System.DateTime.Now;
                }

            }
            catch (Exception)
            {
                return DateTime.Parse("2999-05-01");
            }
            finally
            {
                if (request != null)
                { request.Abort(); }
                if (response != null)
                { response.Close(); }
                if (headerCollection != null)
                { headerCollection.Clear(); }
            }
        }

        /// <summary>
        /// 删除空文件夹
        /// </summary>
        /// <param name="startLocation"></param>
        public static void DeleteEmptyDirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteEmptyDirectories(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        /// <summary>
        /// 检查注册表，看是否已绑定内建的vseditor。 返回2为已绑定
        /// </summary>
        /// <returns></returns>
        public static int CheckBuildinVSEditorInstall()
        {
            int count = 0;
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@".vpy"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("");
                        if (o != null)
                        {
                            if (o.ToString() == "vpy_auto_file")
                            {
                                count += 1;
                            }
                        }
                    }
                }
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"vpy_auto_file\shell\open\command"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("");
                        if (o != null)
                        {
                            string vspath = "\"" + MainWindow.binpath + @"\vs\vsedit.exe" + "\"" + " " + "\"" + @"%1" + "\"";
                            if (o.ToString() == vspath)
                            {
                                count += 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)  //just for demonstration...it's always best to handle specific exceptions
            {
                //react appropriately
            }
            return count;
        }

        /// <summary>
        /// 查找所有控件的子控件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depObj"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindVisualChilds<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChilds<T>(ithChild)) yield return childOfChild;
            }
        }


        /// <summary>
        /// 终止一个指定名称的进程
        /// </summary>
        /// <param name="processName"></param>
        public static void KillProcess(string processName)
        {
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                try
                {
                    // 杀掉这个进程。
                    process.Kill();

                    // 等待进程被杀掉。你也可以在这里加上一个超时时间（毫秒整数）。
                    process.WaitForExit(6000);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // 无法结束进程，可能有很多原因。
                    // 建议记录这个异常，如果你的程序能够处理这里的某种特定异常了，那么就需要在这里补充处理。
                    // Log.Error(ex);
                }
                catch (InvalidOperationException)
                {
                    // 进程已经退出，无法继续退出。既然已经退了，那这里也算是退出成功了。
                    // 于是这里其实什么代码也不需要执行。
                }
            }
        }

        /// <summary>
        /// 返回程序路径下的temp文件夹
        /// </summary>
        /// <returns></returns>
        public static string GetAppTempPath()
        {
            string pstr = MainWindow.binpath + @"\temp\";
            Directory.CreateDirectory(pstr);
            return pstr;
        }

    }
}
