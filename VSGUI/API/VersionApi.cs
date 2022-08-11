using System;
using System.Collections.Generic;
using System.Linq;
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
            string biPyVer = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\vs\", @"python.exe --version");
            string biVsVer = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\vs\", @"vspipe.exe --version");
            string sysPyVer = ProcessApi.RunSyncProcess("", @"python.exe --version");
            string sysVsVer = ProcessApi.RunSyncProcess("", @"vspipe.exe --version");

            var x = Regex.Matches(biPyVer, @"Python (.*)\s");
            if (x.Count > 0)
            {
                versions[0] = x[0].Groups[1].Value;
            }

            x = Regex.Matches(biVsVer, @"VapourSynth(?:.|\s)*?Core (.*)\s");
            if (x.Count > 0)
            {
                versions[1] = x[0].Groups[1].Value;
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


            return versions;
        }

    }
}
