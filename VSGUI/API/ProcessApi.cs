using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VSGUI.API
{
    internal class ProcessApi
    {
        //internal static List<int> ProcessId = new List<int>();
        //internal static bool IsPause = false;
        //internal static int TotalFrames = 0;
        private static string tempOutputStr = "";

        /// <summary>
        /// 停止所有线程
        /// </summary>
        internal static void StopProcessAll()
        {
            var queueList = QueueApi.GetQueueList();
            foreach (var queueListItem in queueList)
            {
                if (queueListItem["processTheadId"].ToString() != "")
                {
                    queueListItem["status"] = "stop";
                    queueListItem["processvalue"] = "0";
                    int itemProcessId = int.Parse(queueListItem["processTheadId"].ToString());
                    queueListItem["processTheadId"] = "";
                    foreach (var item in GetChildProcess(itemProcessId))
                    {
                        var pidt = Process.GetProcessById(item);
                        pidt.Kill();
                    }
                    Process.GetProcessById(itemProcessId).Kill();
                }
            }
            static int[] GetChildProcess(int ParentPID)
            {
                var proc = new ProcessWindowsAPI.WindowsProcess();
                return proc.ListParentChild(ParentPID);
            }
        }

        /// <summary>
        /// 停止单个线程
        /// </summary>
        internal static void StopProcessItem(string queueid)
        {
            string processid = QueueApi.GetQueueListitem(queueid, "processTheadId");
            if (processid != "")
            {
                QueueApi.SetQueueListitem(queueid, "processTheadId", "");
                QueueApi.SetQueueListitem(queueid, "status", "stop");
                QueueApi.SetQueueListitem(queueid, "processvalue", "0");
                int itemProcessId = int.Parse(processid);
                foreach (var item in GetChildProcess(itemProcessId))
                {
                    var pidt = Process.GetProcessById(item);
                    pidt.Kill();
                }
                Process.GetProcessById(itemProcessId).Kill();
            }
            static int[] GetChildProcess(int ParentPID)
            {
                var proc = new ProcessWindowsAPI.WindowsProcess();
                return proc.ListParentChild(ParentPID);
            }
        }

        /// <summary>
        /// 暂停单个线程
        /// </summary>
        internal static void PauseProcessItem(string queueid)
        {
            string processid = QueueApi.GetQueueListitem(queueid, "processTheadId");
            if (processid != "")
            {
                int itemProcessId = int.Parse(processid);
                var proc = new ProcessWindowsAPI.WindowsProcess();
                proc.PauseParentChild(itemProcessId);
                QueueApi.SetQueueListitem(queueid, "status", "pause");
                QueueApi.SetQueueListitem(queueid, "endtime", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString());
            }
        }

        /// <summary>
        /// 继续单个线程
        /// </summary>
        /// <param name="queueid"></param>
        internal static void ResumeProcessItem(string queueid)
        {
            string processid = QueueApi.GetQueueListitem(queueid, "processTheadId");
            if (processid != "")
            {
                int itemProcessId = int.Parse(processid);
                var proc = new ProcessWindowsAPI.WindowsProcess();
                proc.ResumeParentChild(itemProcessId);
                QueueApi.SetQueueListitem(queueid, "status", "running");
                Debug.WriteLine(QueueApi.GetQueueListitem(queueid, "endtime"));
                Debug.WriteLine(QueueApi.GetQueueListitem(queueid, "starttime"));
                Debug.WriteLine(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
                long newstarttime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - (long.Parse(QueueApi.GetQueueListitem(queueid, "endtime")) - long.Parse(QueueApi.GetQueueListitem(queueid, "starttime")));
                Debug.WriteLine(newstarttime);
                QueueApi.SetQueueListitem(queueid, "starttime", newstarttime.ToString());
                QueueApi.SetQueueListitem(queueid, "endtime", "");
            }
        }

        /// <summary>
        /// 运行简单命令，获取结果并返回
        /// </summary>
        /// <param name="common"></param>
        /// <returns></returns>
        public static string RunSyncProcess(string clipath, string common, bool outputUTF8 = false)
        {
            tempOutputStr = "";
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo("cmd")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            if (clipath != "")
            {
                proc.StartInfo.WorkingDirectory = clipath;
            }

            if (outputUTF8)
            {
                proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            }
            proc.StartInfo.StandardErrorEncoding = Encoding.UTF8;


            proc.OutputDataReceived += Proc_DataReceived;
            proc.ErrorDataReceived += Proc_DataReceived;

            proc.EnableRaisingEvents = true;
            proc.Start();
            proc.StandardInput.WriteLine(common + "&exit");

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();


            void Proc_DataReceived(object sender, DataReceivedEventArgs e)
            {
                Debug.WriteLine(e.Data);
                tempOutputStr += e.Data + "\r\n";
            }

            proc.WaitForExit();
            return tempOutputStr;
        }


        public static void RunProcess(string clipath, string common, Action<DataReceivedEventArgs, bool> inDataReceived, Action inExited, out string processid, bool outputUTF8 = false)
        {

            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo("cmd")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            if (clipath != "")
            {
                proc.StartInfo.WorkingDirectory = clipath;
            }

            if (outputUTF8)
            {
                proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            }
            proc.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            proc.OutputDataReceived += Proc_DataReceived;
            proc.ErrorDataReceived += Proc_DataReceived;
            proc.EnableRaisingEvents = true;
            proc.Exited += Proc_Exited;
            proc.Start();
            QueueApi.runningQueueCount += 1;
            processid = proc.Id.ToString();

            proc.StandardInput.WriteLine(common + "&exit");

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            void Proc_Exited(object? sender, EventArgs e)
            {
                QueueApi.runningQueueCount -= 1;
                inExited();
            }

            void Proc_DataReceived(object sender, DataReceivedEventArgs e)
            {
                Debug.WriteLine(e.Data);
                inDataReceived(e, proc.HasExited);
            }
        }

    }


    internal partial class ProcessWindowsAPI
    {
        [Flags()]
        enum ThreadAccess : int
        {
            TERMINATE = 1,
            SUSPEND_RESUME = 2,
            GET_CONTEXT = 8,
            SET_CONTEXT = 16,
            SET_INFORMATION = 32,
            QUERY_INFORMATION = 64,
            SET_THREAD_TOKEN = 128,
            IMPERSONATE = 256,
            DIRECT_IMPERSONATION = 512,
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hHandle);

        internal class WindowsProcess
        {
            internal int[] ListParentChild(int value)
            {
                var plist = new List<int>();
                var query = $"Select * From Win32_Process Where ParentProcessId = {value}";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection processList = searcher.Get();

                foreach (ManagementObject item in processList)
                {
                    plist.Add(int.Parse(item["ProcessId"].ToString()));
                }

                return plist.ToArray();
            }

            internal void PauseParentChild(int value)
            {
                foreach (var pid in ListParentChild(value))
                {
                    var id = Process.GetProcessById(pid);

                    foreach (ProcessThread t in id.Threads)
                    {
                        IntPtr th;
                        th = OpenThread(ThreadAccess.SUSPEND_RESUME, false, Convert.ToUInt32(t.Id));
                        if ((th != IntPtr.Zero))
                        {
                            SuspendThread(th);
                            CloseHandle(th);
                        }
                    }
                }
            }

            internal void ResumeParentChild(int value)
            {
                foreach (var pid in ListParentChild(value))
                {
                    var id = Process.GetProcessById(pid);

                    foreach (ProcessThread t in id.Threads)
                    {
                        IntPtr th;
                        th = OpenThread(ThreadAccess.SUSPEND_RESUME, false, Convert.ToUInt32(t.Id));
                        if ((th != IntPtr.Zero))
                        {
                            ResumeThread(th);
                            CloseHandle(th);
                        }
                    }
                }
            }
        }
    }
}
