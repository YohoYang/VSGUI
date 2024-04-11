using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSGUI.API
{
    internal class AudioApi
    {
        public static string MakeAudioScript(int audioencoderid, string cutstr, string fpsstr, string inputstr, string delay)
        {
            string inputext = Path.GetExtension(inputstr).ToLower();
            if (delay == "") delay = "0";
            //批量输入的参数
            int delayint;
            if (delay == "auto")
            {
                QueueApi.AudioFileInputCheck(inputstr, out string audiodelayboxText, out bool isError);
                delayint = int.Parse(audiodelayboxText);
            }
            else
            {
                delayint = int.Parse(delay);
            }

            bool isNormalize = EncoderApi.GetNormalize("audio", audioencoderid);
            string NormalizeStr = "";
            if (isNormalize) NormalizeStr = @"Normalize()";

            string script = "";
            script += @"LoadPlugin(""LSMASHSource.dll"")" + "\r\n";

            bool isVideoinput = false;
            if (inputext == ".ts" || inputext == ".mkv" || inputext == ".m2ts" || inputext == ".flv")
            {
                isVideoinput = true;
            }
            if (cutstr != "" && fpsstr != "")
            {
                if (isVideoinput)
                {
                    //情况1
                    script += @"__v = LWLibavVideoSource(""" + inputstr + @""")" + "\r\n";
                    script += @"__a = LWLibavAudioSource(""" + inputstr + @""")" + "\r\n";
                    if (delayint != 0) script += @"__a = DelayAudio(__a," + delayint.ToString() + @"/1000.0)" + "\r\n";
                    script += @"AudioDub(__v, __a)" + "\r\n";
                }
                else
                {
                    //情况2
                    script += @"LWLibavAudioSource(""" + inputstr + @""")" + "\r\n";
                    if (delayint != 0) script += @"DelayAudio(" + delayint.ToString() + @"/1000.0)" + "\r\n";
                }

                //需要在此处增加对cut文本的格式检测，仅能存在数字

                string[] cutblock = cutstr.Split("&");
                foreach (var block in cutblock)
                {
                    string[] cutliststr = block.Split("+");
                    for (int i = 0; i < cutliststr.Length; i++)
                    {
                        cutliststr[i] = cutliststr[i].Replace(",", ":").Replace("[", "").Replace("]", "");
                    }
                    script += @"__film = last" + "\r\n";
                    script += @"__just_audio = __film" + "\r\n";
                    script += @"__blank = BlankClip(length=" + int.Parse(cutliststr[cutliststr.Length - 1].Split(":")[1].ToString()) + 1 + ", fps=" + fpsstr + ")" + "\r\n";
                    script += @"__film = AudioDub(__blank, __film)" + "\r\n";
                    for (int i = 0; i < cutliststr.Length; i++)
                    {
                        int startf = int.Parse(cutliststr[i].Split(":")[0].ToString());
                        int endf = int.Parse(cutliststr[i].Split(":")[1].ToString());
                        script += @"__t" + i + @" = __film.trim(" + startf + ", " + endf + ")" + "\r\n";
                    }
                    for (int i = 0; i < cutliststr.Length; i++)
                    {
                        script += @"__t" + i;
                        if (i != cutliststr.Length - 1)
                        {
                            script += " ++ ";
                        }
                        else
                        {
                            script += " \r\n";
                        }
                    }
                    script += @"AudioDubEx(__just_audio, last)" + "\r\n";
                }
                script += NormalizeStr + "\r\n";
                script += @"return last";
            }
            else
            {
                if (isVideoinput)
                {
                    //情况3
                    script += @"__v = LWLibavVideoSource(""" + inputstr + @""")" + "\r\n";
                    script += @"__a = LWLibavAudioSource(""" + inputstr + @""" ,av_sync=true)" + "\r\n";
                    if (delayint != 0) script += @"__a = DelayAudio(__a," + delayint.ToString() + @"/1000.0)" + "\r\n";
                    script += @"AudioDub(__v, __a)" + "\r\n";
                    script += NormalizeStr + "\r\n";
                    script += @"return last";
                }
                else
                {
                    //情况4
                    script += @"LWLibavAudioSource(""" + inputstr + @""")" + "\r\n";
                    if (delayint != 0) script += @"DelayAudio(" + delayint.ToString() + @"/1000.0)" + "\r\n";
                    script += NormalizeStr + "\r\n";
                    script += @"return last";
                }
            }

            return script;
        }

        public static bool CheckCutStrIsError(string cutstr)
        {
            string script = "";
            try
            {
                string[] cutblock = cutstr.Split("&");
                foreach (var block in cutblock)
                {
                    string[] cutliststr = block.Split("+");
                    for (int i = 0; i < cutliststr.Length; i++)
                    {
                        cutliststr[i] = cutliststr[i].Replace(",", ":").Replace("[", "").Replace("]", "");
                    }
                    script += int.Parse(cutliststr[cutliststr.Length - 1].Split(":")[1].ToString()) + "\r\n";
                    for (int i = 0; i < cutliststr.Length; i++)
                    {
                        int startf = int.Parse(cutliststr[i].Split(":")[0].ToString());
                        int endf = int.Parse(cutliststr[i].Split(":")[1].ToString());
                        script += @"__t" + i + @" = __film.trim(" + startf + ", " + endf + ")" + "\r\n";
                    }
                    for (int i = 0; i < cutliststr.Length; i++)
                    {
                        script += @"__t" + i;
                        if (i != cutliststr.Length - 1)
                        {
                            script += " ++ ";
                        }
                        else
                        {
                            script += " \r\n";
                        }
                    }
                    script += @"AudioDubEx(__just_audio, last)" + "\r\n";
                }
            }
            catch (Exception)
            {
                return true;
            }
            return false;
        }
    }
}
