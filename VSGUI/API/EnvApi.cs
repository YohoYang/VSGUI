using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace VSGUI.API
{
    internal class EnvApi
    {
        public static string[] getEnvVersion(string path)
        {
            string[] versions = new string[2];
            if (path == null)
            {
                path = "";
            }
            else
            {
                if (!path.EndsWith(@"\"))
                {
                    path = path + @"\";
                }
            }
            MatchCollection x;
            try
            {
                string PyVer = ProcessApi.RunSyncProcess(path, @"python.exe --version");
                string VsVer = ProcessApi.RunSyncProcess(path, @"vspipe.exe --version");

                x = Regex.Matches(PyVer, @"Python (.*)\s");
                if (x.Count > 0)
                {
                    versions[0] = x[0].Groups[1].Value.Replace("\r\n","").Replace("\r", "").Replace("\n", "");
                }

                x = Regex.Matches(VsVer, @"VapourSynth(?:.|\s)*?Core (.*)\s");
                if (x.Count > 0)
                {
                    versions[1] = x[0].Groups[1].Value.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
                }
            }
            catch (Exception)
            {

            }
            return versions;
        }

        public static int checkEnvEditorNow()
        {
            string envpath = MainWindow.envpath;
            if (envpath == "")//系统环境
            {
                //坏了，系统的editor不好找，直接弹提示
                return 2;
            }
            else
            {
                if (!envpath.EndsWith(@"\"))
                {
                    envpath += @"\";
                }
                if (File.Exists(envpath + @"vsedit.exe"))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
