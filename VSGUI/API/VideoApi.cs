using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VSGUI.API
{
    internal class VideoApi
    {
        public static string MakeVideoScript(string videoinputpath, string resolution, string asspath)
        {
            //输入视频信息检测
            int sourceWidth = 0;
            int sourceHeight = 0;
            bool tfm = false;
            if (!string.IsNullOrEmpty(videoinputpath) && File.Exists(videoinputpath))
            {
                string result = ProcessApi.RunSyncProcess(MainWindow.binpath + @"\tools\ffmpeg\", @"ffmpeg.exe" + " -hide_banner -y -i " + "\"" + videoinputpath + "\"");
                var videoInfo = Regex.Matches(result, @"Stream.*Video:.*");
                if (videoInfo.Count >= 1)
                {
                    result = videoInfo[0].ToString();
                    if (!videoInfo[0].ToString().Contains("attached pic"))
                    {
                        var resolutionInfo = Regex.Matches(result, @", (\d+)x(\d+)");
                        if (resolutionInfo[0].Groups.Count >= 3)
                        {
                            sourceWidth = int.Parse(resolutionInfo[0].Groups[1].ToString());
                            sourceHeight = int.Parse(resolutionInfo[0].Groups[2].ToString());
                        }
                        var sarInfo = Regex.Matches(result, @"SAR (\d+):(\d+)");
                        if (sarInfo[0].Groups.Count >= 3)
                        {
                            sourceWidth = (int)Math.Round((sourceWidth * (double.Parse(sarInfo[0].Groups[1].ToString()) / double.Parse(sarInfo[0].Groups[2].ToString()))) / 2, MidpointRounding.AwayFromZero) * 2;
                        }
                        if (!result.Contains("progressive"))
                        {
                            tfm = true;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            string scriptstr = "";
            scriptstr += @"import vapoursynth as vs" + "\r\n";
            scriptstr += @"core = vs.core" + "\r\n";
            scriptstr += "\r\n";
            scriptstr += @"videosrc = r" + "\"" + videoinputpath + "\"" + "\r\n";
            scriptstr += @"video = core.lsmas.LWLibavSource(videosrc)" + "\r\n";
            scriptstr += "\r\n";
            if (tfm)
            {
                scriptstr += @"video = core.tivtc.TFM(video, order=-1, mode=1, PP=5, slow=2, chroma=False)" + "\r\n";
                scriptstr += @"video = core.tivtc.TDecimate(video, mode=1, cycle=5)" + "\r\n";
            }
            //缩放
            var x1 = Regex.Matches(resolution, @"\d+P");
            if (x1.Count >= 1)
            {
                int newHeight = int.Parse(x1[0].Value.Replace("P", ""));
                sourceWidth = (int)Math.Round((sourceWidth * ((double)newHeight / (double)sourceHeight)) / 2, MidpointRounding.AwayFromZero) * 2;
                sourceHeight = newHeight;
            }
            scriptstr += @"video = core.resize.Lanczos(video, width=" + sourceWidth + ", height=" + sourceHeight + ")" + "\r\n";
            //字幕
            if (asspath != "")
            {
                if (File.Exists(asspath))
                {
                    scriptstr += @"video = core.assrender.TextSub(video, file=r" + "\"" + asspath + "\"" + ")" + "\r\n";
                }
            }
            //输出
            scriptstr += @"video.set_output()" + "\r\n";
            return scriptstr;
        }

        public static void PreviewTempVpy(string script)
        {
            string temppath = Path.GetTempPath() + @"vsgui\preview.vpy";
            File.WriteAllText(temppath, script);
            Process.Start(MainWindow.binpath + @"\vs\vsedit.exe", temppath);
        }
    }
}
