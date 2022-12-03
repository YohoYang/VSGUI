using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
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
        string[,] encoders =
        {
            {"video","x264",".h264|.m4v|.mp4" },
            {"video","x265",".h265|.m4v|.mp4" },
            {"video","nvenc",".h264|.h265|.m4v|.mp4" },
            {"audio","qaac",".aac|.m4a" },
            {"audio","flac",".flac" },
        };

        public EncoderWindow(string type)
        {
            InitializeComponent();
            ltype = type;
            Init();
        }

        private void Init()
        {
            tempObject = EncoderApi.GetEncoderJsonObject();
            if (ltype == "video")
            {
                encoderbox.ItemsSource = GetEncoderData("video");
                encodertypebox.ItemsSource = GetEncodersType();
                string getconfig = IniApi.IniReadValue("videoencoderboxSelectedIndex");
                if (getconfig == "") getconfig = "0";
                encoderbox.SelectedIndex = int.Parse(getconfig);
                normalizebox.Visibility = Visibility.Collapsed;
            }
            else
            {
                encoderbox.ItemsSource = GetEncoderData("audio");
                encodertypebox.ItemsSource = GetEncodersType();
                string getconfig = IniApi.IniReadValue("audioencoderboxSelectedIndex");
                if (getconfig == "") getconfig = "0";
                encoderbox.SelectedIndex = int.Parse(getconfig);
            }
        }

        private void UpdateEncoderData()
        {
            if (ltype == "video")
            {
                encoderbox.ItemsSource = GetEncoderData("video");
            }
            else
            {
                encoderbox.ItemsSource = GetEncoderData("audio");
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
            namebox.Text = jsonObj[ltype][selectIndex]["name"].ToString();
            encodertypebox.Text = jsonObj[ltype][selectIndex]["encodername"].ToString();
            parameterbox.Text = jsonObj[ltype][selectIndex]["parameter"].ToString();
            suffixbox.ItemsSource = GetEncodersSuffix(encodertypebox.Text);
            suffixbox.Text = jsonObj[ltype][selectIndex]["suffix"].ToString();
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
        }

        private void encodertypebox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (encodertypebox.SelectedItem == null)
            {
                return;
            }
            suffixbox.ItemsSource = GetEncodersSuffix(encodertypebox.SelectedItem.ToString());
            suffixbox.SelectedIndex = 0;
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
            return list;
        }


        private List<string> GetEncoderData(string type)
        {
            var encoderJson = tempObject;
            if (encoderJson == null)
            {
                return null;
            }
            List<string> ProfileLists = new List<string>();
            for (int i = 0; i < encoderJson[type].AsArray().Count; i++)
            {
                string namestr = encoderJson[type][i]["encodername"].ToString() + "-" + encoderJson[type][i]["name"].ToString();
                if (encoderJson[type][i].AsObject().ContainsKey("tag"))
                {
                    if (encoderJson[type][i]["tag"].ToString() == "server")
                    {
                        namestr = @"[s] " + namestr;
                    }
                }
                ProfileLists.Add(namestr);
            }
            return ProfileLists;
        }

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
                        list.Add(item);
                    }
                    break;
                }
            }
            return list;
        }

        private void Addbutton_Click(object sender, RoutedEventArgs e)
        {
            JsonObject obj = new JsonObject();
            obj.Add("name", "new profiles");
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
            JsonObject obj = new JsonObject();
            obj.Add("name", namebox.Text);
            obj.Add("encodername", encodertypebox.Text);
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
        }
    }
}
