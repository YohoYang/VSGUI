using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VSGUI.API
{
    class IniApi
    {
        public static string inipath = MainWindow.binpath + @"\" + "config.ini";

        //声明API函数

        [DllImport("kernel32")]
        public static extern bool WritePrivateProfileString(byte[] section, byte[] key, byte[] val, string filePath);

        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(byte[] section, byte[] key, byte[] def, byte[] retVal, int size, string filePath);

        /// <summary> 
        /// 读出INI文件 
        /// </summary> 
        /// <param name="Section">项目名称(如 [TypeName] )</param> 
        /// <param name="Key">键</param> 
        public static string IniReadValue(string key)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int count = GetPrivateProfileString(getBytes("Config", "utf-8"), getBytes(key, "utf-8"), getBytes("", "utf-8"), buffer, 1024, inipath);
                return Encoding.GetEncoding("utf-8").GetString(buffer, 0, count).Trim();
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary> 
        /// 写入INI文件 
        /// </summary> 
        /// <param name="Section">项目名称(如 [TypeName] )</param> 
        /// <param name="Key">键</param> 
        /// <param name="Value">值</param> 
        public static bool IniWriteValue(string key, string value)
        {
            return WritePrivateProfileString(getBytes("Config", "utf-8"), getBytes(key, "utf-8"), getBytes(value, "utf-8"), inipath);
        }

        private static byte[] getBytes(string s, string encodingName)
        {
            return null == s ? null : Encoding.GetEncoding(encodingName).GetBytes(s);
        }

        /// <summary> 
        /// 验证文件是否存在 
        /// </summary> 
        /// <returns>布尔值</returns> 
        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }
    }
}
