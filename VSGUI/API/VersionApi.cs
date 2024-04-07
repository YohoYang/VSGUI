using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VSGUI.API
{
    internal class VersionApi
    {
        public static string[] GetVersion()
        {
            string[] versions = new string[4];
            MatchCollection x;
            string sysPyVer;
            string sysVsVer;
            if (CommonApi.CheckCustomPyenvExec() && IniApi.IniReadValue("CustomPyenv") != "false")
            {
                string CustomPyenvDir = CommonApi.GetCustomPyenvDir();
                string pythonExePath = Path.Combine(CustomPyenvDir, "python.exe");
                string vspipeExePath = Path.Combine(CustomPyenvDir, "vspipe.exe");
                sysPyVer = ProcessApi.RunSyncProcess("", $"{pythonExePath} --version");
                sysVsVer = ProcessApi.RunSyncProcess("", $"{vspipeExePath} --version");
            }
            else 
            { 
            sysPyVer = ProcessApi.RunSyncProcess("", @"python.exe --version");
            sysVsVer = ProcessApi.RunSyncProcess("", @"vspipe.exe --version");
            }
            x = Regex.Matches(sysPyVer, @"Python (.*)\s");
            if (x.Count > 0)
            {
                versions[2] = x[0].Groups[1].Value;
            }

            x = Regex.Matches(sysVsVer, @"VapourSynth(?:.|\s)*?Core (.*)\s");
            if (x.Count > 0)
            {
                versions[3] = x[0].Groups[1].Value;
            }

            try
            {
                string biPyVer = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\vs\", @"python.exe --version");
                string biVsVer = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\vs\", @"vspipe.exe --version");

                x = Regex.Matches(biPyVer, @"Python (.*)\s");
                if (x.Count > 0)
                {
                    versions[0] = x[0].Groups[1].Value;
                }

                x = Regex.Matches(biVsVer, @"VapourSynth(?:.|\s)*?Core (.*)\s");
                if (x.Count > 0)
                {
                    versions[1] = x[0].Groups[1].Value;
                }
            }
            catch (Exception)
            {

            }

            return versions;
        }

    }
}
