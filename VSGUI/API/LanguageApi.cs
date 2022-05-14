using System;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace VSGUI.API
{
    /// <summary>
    /// 多语言操作类
    /// </summary>
    public class LanguageApi
    {
        private static Dictionary<string, string> map;

        private LanguageApi() { }

        /// <summary>
        /// 使用 key - value形式初始化多语言控制库，使用前必须执行
        /// </summary>
        /// <param name="ht">key:string; value:string</param>
        public static void Init()
        {
            map = new Dictionary<string, string>();
            map.Add("简体中文", @"zh-cn");
            map.Add("English", @"en");
            if (IniApi.IniReadValue("Language") == "")
            {
                SystemLanguageCheck();
            }
            else
            {
                SwitchLanguage(IniApi.IniReadValue("Language"));
            }
        }

        public static List<string> GetLanguagesList()
        {
            List<string> list = new List<string>();
            foreach (string language in map.Keys)
            {
                list.Add(language);
            }
            return list;
        }

        public static void SystemLanguageCheck()
        {
            string lang = System.Threading.Thread.CurrentThread.CurrentCulture.Name.ToLower();
            if (map.ContainsValue(lang))
            {
                foreach (var item in map)
                {
                    if (item.Value == lang)
                    {
                        SwitchLanguage(item.Key);
                        break;
                    }
                }
            }
            else
            {
                SwitchLanguage("English");
            }
        }

        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="key">Hashtable下存在的key，根据此key获取对应的资源文件</param>
        public static void SwitchLanguage(string key)
        {
            if (null == map)
            {
                Init();
            }
            if (!map.ContainsKey(key))
            {
                key = "English";
            }
            IniApi.IniWriteValue("Language", key);
            var value = map[key];
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new Uri(@"pack://application:,,,/VSGUI;component/Properties/Langs/" + value + @".xaml");
            Application.Current.Resources.MergedDictionaries[0] = resourceDictionary;
        }

        /// <summary>
        /// 获取当前使用的语言在Hashtable的key的表现形式
        /// </summary>
        public static string CurrentLanguageKey()
        {
            if (null == map)
            {
                throw new NullReferenceException("初始化多语言Hashtable后使用");
            }
            string value = Application.Current.Resources.MergedDictionaries[0].Source.OriginalString;

            try
            {
                return (from obj in map.Keys where map[obj].Equals(value) select obj).FirstOrDefault();
            }
            catch
            {
                throw new NullReferenceException(string.Format("Hashtable未匹配到当前语言"));
            }
        }

        /// <summary>
        /// 根据资源key获取对应的资源值
        /// </summary>
        /// <param name="resourseKey">资源key，非Hashtable的key</param>
        /// <returns>获取到当前使用的语言对应key下的值，取不到返回空字符串</returns>
        public static string FindRes(string resourseKey)
        {
            string? resstr = Application.Current.Resources[resourseKey] as string;
            if (resstr == null)
            {
                resstr = "<" + resourseKey + ">";
            }
            return resstr;
        }

    }

}
