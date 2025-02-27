﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VSGUI.API;

namespace VSGUI
{
    /// <summary>
    /// EncoderWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EncoderWindow : Window
    {
        JsonObject tempObject;
        string ltype;
        public static string[,] encoders;

        public static void EncoderInit()
        {
            encoders = new [,]{
                {"video","x264",".h264|.m4v|.mp4",@"\bin\encoder\x264\x264.exe","--demuxer y4m","- -o" },
                {"video","x265",".h265|.m4v|.mp4",@"\bin\encoder\x265\x265.exe","--y4m" ,"- -o"},
                {"video","nvenc",".h264|.h265|.m4v|.mp4",@"\bin\encoder\NVEncC\NVEncC64.exe","--y4m -i -","-o" },
                {"video","ffmpeg","",@"\bin\encoder\ffmpeg\ffmpeg.exe","-y -i -","" },
                {"audio","qaac",".aac|.m4a",@"\bin\encoder\qaac\qaac64.exe","" , "- -o"},
                {"audio","ffmpeg","",@"\bin\encoder\ffmpeg\ffmpeg.exe","-y -i -","" },
                {"audio",LanguageApi.FindRes("p053"),".wav",@"\bin\encoder\ffmpeg\ffmpeg.exe","-y -i -","" },
            };
        }
        
        public EncoderWindow(string type)
        {
            InitializeComponent();
            ltype = type;
            EncoderInit();
            Init();
        }

        private void Init()
        {
            tempObject = EncoderApi.GetEncoderJsonObject();
            string tab = IniApi.IniReadValue("encodeTab");
            if (ltype == "video")
            {
                encoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(tempObject, "video");
                encodertypebox.ItemsSource = GetEncodersType();
                string getconfig = IniApi.IniReadValue("videoencoderboxSelectedIndex");
                if (tab=="simple")
                {
                    getconfig = IniApi.IniReadValue("simplevideoencoderboxSelectedIndex");
                }
                if (getconfig == "") getconfig = "0";
                encoderbox.SelectedIndex = int.Parse(getconfig);
                normalizebox.Visibility = Visibility.Collapsed;
                this.encoderwin.Title = LanguageApi.FindRes("p010");
            }
            else
            {
                encoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(tempObject, "audio");
                encodertypebox.ItemsSource = GetEncodersType();
                string getconfig = IniApi.IniReadValue("audioencoderboxSelectedIndex");
                if (tab == "simple")
                {
                    getconfig = IniApi.IniReadValue("simpleaudioencoderboxSelectedIndex");
                }
                if (getconfig == "") getconfig = "0";
                encoderbox.SelectedIndex = int.Parse(getconfig);
                this.encoderwin.Title = LanguageApi.FindRes("p011");
            }
        }

        private void UpdateEncoderData()
        {
            if (ltype == "video")
            {
                encoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(tempObject, "video");
            }
            else
            {
                encoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(tempObject, "audio");
            }
        }

        private void encoderbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var jsonObj = tempObject;
            int selectIndex = ((ComboBox)sender).SelectedIndex;
            if (selectIndex == -1)
            {
                if (encoderbox.Items.Count > 0)
                {
                    selectIndex = 0;
                    ((ComboBox)sender).SelectedIndex = 0;
                }
                else
                {
                    deletebutton.IsEnabled = false;
                    savebutton.IsEnabled = false;
                    return;
                }
            }
            namebox.Text = GetEncoderData(jsonObj, selectIndex, "name");
            if (GetEncoderData(jsonObj, selectIndex, "encodername") == "c")
            {
                encodertypebox.Text = LanguageApi.FindRes("p009");
            }
            else 
            if (GetEncoderData(jsonObj, selectIndex, "encodername") == "copy")
            {
                encodertypebox.Text = LanguageApi.FindRes("p053");
            }
            else
            {
                encodertypebox.Text = GetEncoderData(jsonObj, selectIndex, "encodername");
            }
            encoderpathbox.Text = GetEncoderData(jsonObj, selectIndex, "encoderpath");
            pipeinputformatbox.Text = GetEncoderData(jsonObj, selectIndex, "pipeinputformat");
            outputformatbox.Text = GetEncoderData(jsonObj, selectIndex, "outputformat");
            parameterbox.Text = GetEncoderData(jsonObj, selectIndex, "parameter");
            suffixbox.ItemsSource = GetEncodersSuffix(encodertypebox.Text);
            suffixbox.Text = GetEncoderData(jsonObj, selectIndex, "suffix");
            if (ltype == "audio")
            {
                normalizebox.IsChecked = bool.Parse(jsonObj[ltype][selectIndex]["normalize"].ToString());
            }
            deletebutton.IsEnabled = true;
            savebutton.IsEnabled = true;
            buttondesc.Text = "";
            if (jsonObj[ltype][selectIndex].AsObject().ContainsKey("tag"))
            {
                if (jsonObj[ltype][selectIndex]["tag"].ToString() == "server")
                {
                    deletebutton.IsEnabled = false;
                    savebutton.IsEnabled = false;
                    buttondesc.Text = LanguageApi.FindRes("netEncoderProtectDesc");
                }
            }
            //老版本的配置的兼容性处理
            if (encodertypebox.Text != LanguageApi.FindRes("p009"))
            {
                if (encoderpathbox.Text == "")
                {
                    encoderpathbox.Text = GetEncoderPath(encodertypebox.Text);
                }
                if (pipeinputformatbox.Text == "")
                {
                    pipeinputformatbox.Text = GetEncoderPipeinputformat(encodertypebox.Text);
                }
                if (outputformatbox.Text == "")
                {
                    outputformatbox.Text = GetEncoderOutputformat(encodertypebox.Text);
                }
            }

            string GetEncoderData(JsonObject jsonObj, int selectIndex, string keyname)
            {
                if (jsonObj[ltype][selectIndex][keyname] == null)
                {
                    return "";
                }
                else
                {
                    return jsonObj[ltype][selectIndex][keyname].ToString();
                }
            }

        }




        private void encodertypebox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (encodertypebox.SelectedItem == null)
            {
                return;
            }
            BrushConverter brushConverter = new BrushConverter();

            this.encoderpathbox.IsReadOnly = false;
            this.pipeinputformatbox.IsReadOnly = false;
            this.outputformatbox.IsReadOnly = false;
            this.parameterbox.IsReadOnly = false;

            suffixbox.ItemsSource = GetEncodersSuffix(encodertypebox.SelectedItem.ToString());
            suffixbox.SelectedIndex = 0;
            suffixbox.IsEnabled = true;
            
            if (encodertypebox.SelectedIndex >= 0 && encodertypebox.SelectedValue.ToString().Equals(LanguageApi.FindRes("p009")))
            {
                this.encoderpathbox.Foreground = (Brush)brushConverter.ConvertFromString("#212121");
                this.pipeinputformatbox.Foreground = (Brush)brushConverter.ConvertFromString("#212121");
                this.outputformatbox.Foreground = (Brush)brushConverter.ConvertFromString("#212121");
            }
            else if (encodertypebox.SelectedIndex >= 0 && encodertypebox.SelectedValue.ToString().Equals(LanguageApi.FindRes("p053")))
            {
                this.encoderpathbox.IsReadOnly = true;
                this.encoderpathbox.Foreground = (Brush)brushConverter.ConvertFromString("#808080");
                this.pipeinputformatbox.IsReadOnly = true;
                this.pipeinputformatbox.Foreground = (Brush)brushConverter.ConvertFromString("#808080");
                this.outputformatbox.IsReadOnly = true;
                this.outputformatbox.Foreground = (Brush)brushConverter.ConvertFromString("#808080");

                suffixbox.IsEnabled = false;
                this.parameterbox.Text = "-vn -c:a copy";
                this.parameterbox.IsReadOnly = true;
                this.parameterbox.Foreground = (Brush)brushConverter.ConvertFromString("#808080");

            }
            else
            {
                this.encoderpathbox.IsReadOnly = true;
                this.encoderpathbox.Foreground = (Brush)brushConverter.ConvertFromString("#808080");
                this.pipeinputformatbox.IsReadOnly = true;
                this.pipeinputformatbox.Foreground = (Brush)brushConverter.ConvertFromString("#808080");
                this.outputformatbox.IsReadOnly = true;
                this.outputformatbox.Foreground = (Brush)brushConverter.ConvertFromString("#808080");
            }
            encoderpathbox.Text = GetEncoderPath(encodertypebox.SelectedValue.ToString());
            pipeinputformatbox.Text = GetEncoderPipeinputformat(encodertypebox.SelectedValue.ToString());
            outputformatbox.Text = GetEncoderOutputformat(encodertypebox.SelectedValue.ToString());
        }

        private List<string> GetEncodersType()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < encoders.GetLength(0); i++)
            {
                if (encoders[i, 0].Equals(ltype))
                {
                    list.Add(encoders[i, 1].ToString());
                }
            }
            list.Add(LanguageApi.FindRes("p009"));
            return list;
        }


        //private List<string> GetEncoderData(string type)
        //{
        //    var encoderJson = tempObject;
        //    if (encoderJson == null)
        //    {
        //        return null;
        //    }
        //    List<string> ProfileLists = new List<string>();
        //    for (int i = 0; i < encoderJson[type].AsArray().Count; i++)
        //    {
        //        //增加自定义的显示
        //        string encoderName = encoderJson[type][i]["encodername"].ToString();
        //        if (encoderJson[type][i]["encodername"].ToString() == "c")
        //        {
        //            encoderName = encoderJson[type][i]["encoderpath"].ToString().Substring(encoderJson[type][i]["encoderpath"].ToString().LastIndexOf(@"\") + 1);
        //        }
        //        string namestr = encoderName + "-" + encoderJson[type][i]["name"].ToString();
        //        if (encoderJson[type][i].AsObject().ContainsKey("tag"))
        //        {
        //            if (encoderJson[type][i]["tag"].ToString() == "server")
        //            {
        //                namestr = @"[s] " + namestr;
        //            }
        //        }
        //        ProfileLists.Add(namestr);
        //    }
        //    return ProfileLists;
        //}

        private List<string> GetEncodersSuffix(string encodername)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < encoders.GetLength(0); i++)
            {
                if (encoders[i, 0].Equals(ltype) && encoders[i, 1].Equals(encodername))
                {
                    string[] spl = encoders[i, 2].Split('|');
                    foreach (var item in spl)
                    {
                        if (item != "")
                        {
                            list.Add(item);
                        }
                    }
                    break;
                }
            }
            if ((encodertypebox.SelectedIndex >= 0 && encodertypebox.SelectedValue.ToString().Equals(LanguageApi.FindRes("p009"))) || list.Count == 0)
            {
                this.suffixbox.IsEditable = true;
            }
            else
            {
                this.suffixbox.IsEditable = false;
            }

            return list;
        }

        private string GetEncoderPath(string encodername)
        {

            if (!(encodertypebox.SelectedIndex >= 0 && encodertypebox.SelectedValue.ToString().Equals(LanguageApi.FindRes("p009"))))
            {
                for (int i = 0; i < encoders.GetLength(0); i++)
                {
                    if (encoders[i, 0].Equals(ltype) && encoders[i, 1].Equals(encodername))
                    {
                        return encoders[i, 3];
                    }
                }
            }
            return "";
        }

        public static string GetEncoderPathByCode(string type,string name)
        {
            for (int i = 0; i < encoders.GetLength(0); i++)
            {
                if (encoders[i, 0].Equals(type) && encoders[i, 1].Equals(name))
                {
                    return encoders[i, 3];
                }
            }
            return "";
        }

        private string GetEncoderPipeinputformat(string encodername)
        {

            if (!(encodertypebox.SelectedIndex >= 0 && encodertypebox.SelectedValue.ToString().Equals(LanguageApi.FindRes("p009"))))
            {
                for (int i = 0; i < encoders.GetLength(0); i++)
                {
                    if (encoders[i, 0].Equals(ltype) && encoders[i, 1].Equals(encodername))
                    {
                        return encoders[i, 4];
                    }
                }
            }
            return "";
        }

        private string GetEncoderOutputformat(string encodername)
        {

            if (!(encodertypebox.SelectedIndex >= 0 && encodertypebox.SelectedValue.ToString().Equals(LanguageApi.FindRes("p009"))))
            {
                for (int i = 0; i < encoders.GetLength(0); i++)
                {
                    if (encoders[i, 0].Equals(ltype) && encoders[i, 1].Equals(encodername))
                    {
                        return encoders[i, 5];
                    }
                }
            }
            return "";
        }

        private void Addbutton_Click(object sender, RoutedEventArgs e)
        {
            JsonObject obj = new JsonObject();
            obj.Add("name", "new profiles");
            obj.Add("encoderpath", "");
            obj.Add("pipeinputformat", "");
            obj.Add("outputformat", "");
            obj.Add("encodername", "");
            obj.Add("parameter", "");
            obj.Add("suffix", "");
            if (ltype == "audio")
            {
                obj.Add("normalize", false);
            }
            tempObject[ltype].AsArray().Add(obj);
            UpdateEncoderData();
            encoderbox.SelectedIndex = encoderbox.Items.Count - 1;
        }

        private void Deletebutton_Click(object sender, RoutedEventArgs e)
        {
            var newobj = EncoderApi.GetEncoderJsonObject();
            int customcount = 0;
            foreach (JsonObject item in newobj[ltype].AsArray())
            {
                if (item["tag"].ToString() == "custom")
                {
                    customcount++;
                }
            }
            if (customcount <= 1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("needToKeepOneCustomProfile"), LanguageApi.FindRes("error"));
                return;
            }
            if (encoderbox.SelectedIndex < newobj[ltype].AsArray().Count)
            {
                newobj[ltype].AsArray().RemoveAt(encoderbox.SelectedIndex);
            }
            tempObject = newobj;
            EncoderApi.SetEncoderJsonObject(newobj);
            UpdateEncoderData();
            encoderbox.SelectedIndex = encoderbox.Items.Count - 1;
        }

        private void Savebutton_Click(object sender, RoutedEventArgs e)
        {
            if (namebox.Text == "new profiles")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("profileNameCantBeDefault"), LanguageApi.FindRes("error"));
                return;
            }
            if (namebox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("profileNameCantBeEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (encodertypebox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("profileTypeCantBeEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (suffixbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("profileOutputFormatCantBeEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (!suffixbox.Text.StartsWith("."))
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p013"), LanguageApi.FindRes("error"));
                return;
            }

            if (encoderpathbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p012"), LanguageApi.FindRes("error"));
                return;
            }
            else
            {
                if (encoderpathbox.Text.Contains(':'))
                {
                    //绝对
                    if (!File.Exists(encoderpathbox.Text))
                    {
                        MessageBoxApi.Show(LanguageApi.FindRes("p015"), LanguageApi.FindRes("error"));
                        return;
                    }
                } else
                {
                    //相对
                    string pathtext = System.IO.Directory.GetCurrentDirectory() + encoderpathbox.Text;
                    if (!File.Exists(pathtext))
                    {
                        MessageBoxApi.Show(LanguageApi.FindRes("p015"), LanguageApi.FindRes("error"));
                        return;
                    }
                }
            }
            JsonObject obj = new JsonObject();
            obj.Add("name", namebox.Text);
            if (encodertypebox.Text == LanguageApi.FindRes("p009"))
            {
                obj.Add("encodername", "c");
            }
            else if (encodertypebox.Text == LanguageApi.FindRes("p053"))
            {
                obj.Add("encodername", "copy");
            }
            else
            {
                obj.Add("encodername", encodertypebox.Text);
            }
            //保存时不用管是不是相对路径
            obj.Add("encoderpath", encoderpathbox.Text);
            obj.Add("pipeinputformat", pipeinputformatbox.Text);
            obj.Add("outputformat", outputformatbox.Text);
            obj.Add("parameter", parameterbox.Text);
            obj.Add("suffix", suffixbox.Text);
            if (ltype == "audio")
            {
                obj.Add("normalize", normalizebox.IsChecked);
            }
            obj.Add("tag", "custom");
            var newobj = EncoderApi.GetEncoderJsonObject();
            if (encoderbox.SelectedIndex < newobj[ltype].AsArray().Count)
            {
                //已有
                newobj[ltype][encoderbox.SelectedIndex] = obj;
            }
            else
            {
                newobj[ltype].AsArray().Add(obj);
            }
            tempObject = newobj;
            EncoderApi.SetEncoderJsonObject(newobj);
            int lastselect = encoderbox.SelectedIndex;
            UpdateEncoderData();
            encoderbox.SelectedIndex = lastselect;
            this.savebutton.Content = LanguageApi.FindRes("p014");
            this.savebutton.IsEnabled = false;
            new Thread(
               () =>
               {
                   Thread.Sleep(2000);
                   Dispatcher.Invoke(() =>
                   {
                       this.savebutton.Content = LanguageApi.FindRes("save");
                       this.savebutton.IsEnabled = true;
                   });
               }
               ).Start();
        }

        private void encoderpathbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BrushConverter brushConverter = new BrushConverter();
            if (encoderpathbox.Text.Contains(':'))
            {
                //绝对
                if (!File.Exists(encoderpathbox.Text))
                {
                    this.encoderpathbox.BorderBrush = (Brush)brushConverter.ConvertFromString("#EC1414");
                }
                else
                {
                    this.encoderpathbox.BorderBrush = (Brush)brushConverter.ConvertFromString("#46D021");
                }
            }
            else
            {
                //相对
                string pathtext = System.IO.Directory.GetCurrentDirectory() + encoderpathbox.Text;
                if (!File.Exists(pathtext))
                {
                    this.encoderpathbox.BorderBrush = (Brush)brushConverter.ConvertFromString("#EC1414");
                }
                else
                {
                    this.encoderpathbox.BorderBrush = (Brush)brushConverter.ConvertFromString("#46D021");
                }
            }
        }
    }
}
