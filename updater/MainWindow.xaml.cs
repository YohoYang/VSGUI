using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //updater改为仅更新主程序
        public static string localpath = Directory.GetCurrentDirectory() + @"\bin"; //对于updater来说现在就在bin里了

        public MainWindow()
        {
            InitializeComponent();

            System.Diagnostics.Process[] pro = System.Diagnostics.Process.GetProcesses();//获取已开启的所有进程
            //遍历所有查找到的进程
            for (int i = 0; i < pro.Length; i++)
            {
                //判断此进程是否是要查找的进程
                if (pro[i].ProcessName.ToString() == "VSGUI")
                {
                    pro[i].Kill();//结束进程
                    Thread.Sleep(100);
                }
            }

            if (File.Exists(localpath + @"\update\VSGUI.exe.update.7z"))
            {
                RunSyncProcess(localpath, @"7z.exe -y x " + "\"" + localpath + @"\update\VSGUI.exe.update.7z" + "\"" + @" -o" + "\"" + Directory.GetCurrentDirectory() + "\"");
                File.Delete(localpath + @"\update\VSGUI.exe.update.7z");
            }

            Thread.Sleep(100);
            Process.Start(Directory.GetCurrentDirectory() + @"\VSGUI.exe");
            Close();

        }

        public static string RunSyncProcess(string clipath, string common)
        {
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo("cmd")
                {
                    WorkingDirectory = clipath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            proc.Start();
            proc.StandardInput.WriteLine(common);
            proc.StandardInput.WriteLine("exit");
            proc.WaitForExit();

            string outputstr = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            return outputstr + error;
        }

        /// <summary>
        /// 获取目录下所有文件
        /// </summary>
        /// <param name = "path" > 指定目录 </ param >
        /// < returns ></ returns >
        public static string[] GetAllFileInFolder(string path)
        {
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
    }
}
