using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using VSGUI.API;

namespace VSGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static string binpath = Directory.GetCurrentDirectory() + @"\bin";
        private bool forcedStop = false;
        private string coreversion = "v0.4.2";
        public static string logBoxStr = "";

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            LanguageApi.Init();

            System.Diagnostics.Process[] thisProcesses = System.Diagnostics.Process.GetProcessesByName("VSGUI");//获取指定的进程名   
            if (thisProcesses.Length > 1) //如果可以获取到知道的进程名则说明已经启动
            {
                MessageBox.Show(LanguageApi.FindRes("programNotAllowedToRunRepeatedly"), LanguageApi.FindRes("error"));
                this.Close();
                return;
            }

            windowcontrol.Title = windowcontrol.Title + " " + coreversion;
            coreversionshowtextblock.Text = coreversion;

            //检测更新
            UpdateApi.UpdateCheck(UpdateProgressCall);

            //显示队列
            UpdateQueueList();

            //恢复设置的状态
            ResumeConfig();

            //读取编码器配置
            ReCheckEncoderProfiles();

            //更新vseditor关联状态
            UpdateVseditorButtonStatus();

            //获取py和vs版本的情况
            UpdateVersion();
        }

        /// <summary>
        /// 读取编码器配置
        /// </summary>
        private void ReCheckEncoderProfiles()
        {
            UpdateEncoderProfiles();
            string encoderJsonUrl = UseNetEncoderJsonBox.Text;
            new Thread(
                () =>
                {
                    UpdateApi.UpdateEncoderProfiles(encoderJsonUrl, () =>
                    {
                        Dispatcher.Invoke(() =>
                            {
                                UpdateEncoderProfiles();
                                UseNetEncoderJsonDesc.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                                UseNetEncoderJsonDesc.Text = LanguageApi.FindRes("netEncoderUpdateSuccess");
                            });
                    }, () =>
                     {
                         Dispatcher.Invoke(() =>
                         {
                             UseNetEncoderJsonDesc.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                             UseNetEncoderJsonDesc.Text = LanguageApi.FindRes("netEncoderUpdateError");
                         });

                     });
                }
            ).Start();
        }

        /// <summary>
        /// 恢复设置的状态
        /// </summary>
        private void ResumeConfig()
        {
            //语言
            LanguageComboBox.ItemsSource = LanguageApi.GetLanguagesList();
            LanguageComboBox.Text = IniApi.IniReadValue("Language");
            //通用设置项
            foreach (var item in CommonApi.FindVisualChilds<CheckBox>(ConfigContainer))
            {
                if ((item as CheckBox).Name.ToString() != "")
                {
                    if (IniApi.IniReadValue((item as CheckBox).Name.ToString()) == "true")
                    {
                        ((CheckBox)this.FindName((item as CheckBox).Name.ToString())).IsChecked = true;
                    }
                }
            }
            //通用设置项-默认值
            if (IniApi.IniReadValue("AutoUpdate") == "") AutoUpdate.IsChecked = true;
            if (IniApi.IniReadValue("AutoGenerateCut") == "") AutoGenerateCut.IsChecked = true;
            if (IniApi.IniReadValue("UseNetEncoderJson") == "") UseNetEncoderJson.IsChecked = true;
            if (IniApi.IniReadValue("EnableQueueLog") == "") EnableQueueLog.IsChecked = true;
            //封装格式记忆
            if (IniApi.IniReadValue("muxsuffixbox") != "")
            {
                muxsuffixbox.SelectedIndex = int.Parse(IniApi.IniReadValue("muxsuffixbox"));
                simplemuxsuffixbox.SelectedIndex = int.Parse(IniApi.IniReadValue("muxsuffixbox"));
            }
            else
            {
                muxsuffixbox.SelectedIndex = 0;
            }
            //自动更新编码器
            if (IniApi.IniReadValue("UseNetEncoderJsonBox") == "")
            {
                UseNetEncoderJsonBox.Text = @"https://cloud.sbsub.com/vsgui/sbsubprofiles.json";
            }
            else
            {
                UseNetEncoderJsonBox.Text = IniApi.IniReadValue("UseNetEncoderJsonBox");
            }
            //首页压制模式页签记忆
            if (new[] { "", "simple" }.Contains(IniApi.IniReadValue("encodeTab")))
            {
                simpleEncodeButton.IsChecked = true;
            }
            else
            {
                advancedEncodeButton.IsChecked = true;
            }
        }

        /// <summary>
        /// 从json更新编码器预置参数
        /// </summary>
        private void UpdateEncoderProfiles()
        {
            Dispatcher.Invoke(() =>
            {
                videoencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles("video");
                simplevideoencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles("video");
                audioencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles("audio");
                simpleaudioencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles("audio");
                string getconfig;
                getconfig = IniApi.IniReadValue("videoencoderboxSelectedIndex");
                if (getconfig == "" || getconfig == "-1") getconfig = "0";
                videoencoderbox.SelectedIndex = int.Parse(getconfig);
                getconfig = IniApi.IniReadValue("simplevideoencoderboxSelectedIndex");
                if (getconfig == "" || getconfig == "-1") getconfig = "0";
                simplevideoencoderbox.SelectedIndex = int.Parse(getconfig);
                getconfig = IniApi.IniReadValue("audioencoderboxSelectedIndex");
                if (getconfig == "" || getconfig == "-1") getconfig = "0";
                audioencoderbox.SelectedIndex = int.Parse(getconfig);
                getconfig = IniApi.IniReadValue("simpleaudioencoderboxSelectedIndex");
                if (getconfig == "" || getconfig == "-1") getconfig = "0";
                simpleaudioencoderbox.SelectedIndex = int.Parse(getconfig);
            });
        }

        public void UpdateQueueList()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    int lastSelected = QueueListView.SelectedIndex;
                    var queueItemData = QueueApi.GetQueueMember();
                    //更新左侧按钮状态
                    if (QueueApi.runningQueueCount > 0)
                    {
                        StartQueueAll.IsEnabled = false;
                        StopQueueAll.IsEnabled = true;
                    }
                    else
                    {
                        StartQueueAll.IsEnabled = true;
                        StopQueueAll.IsEnabled = false;
                    }
                    //如果列表空，显示一个东西
                    if (queueItemData.Count > 0)
                    {
                        QueueListEmptyTips.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        QueueListEmptyTips.Visibility = Visibility.Visible;
                    }
                    //计算要恢复的选择会不会超出
                    if (lastSelected >= queueItemData.Count)
                    {
                        lastSelected = queueItemData.Count - 1;
                    }
                    QueueListView.ItemsSource = queueItemData;
                    QueueListView.SelectedIndex = lastSelected;
                    QueueTabHeaderNum.Text = "(" + queueItemData.Count + ")";
                    queueinfotext.Text = QueueApi.GetQueueInfoText();
                });
            }
            catch (Exception)
            {
                CommonApi.TryDeleteFile(MainWindow.binpath + @"\json\queueList.json");
                UpdateQueueList();
                return;
            }
        }


        /// <summary>
        /// 处理textbox的拖入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void TextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] filename = (string[])e.Data.GetData(DataFormats.FileDrop);
            ((TextBox)sender).Text = filename[0];
            if (((TextBox)sender).Name == "simplevideoinputbox")
            {
                SimpleVideoInputUpdate();
            }
            else if (((TextBox)sender).Name == "simpleaudioinputbox")
            {
                if (simplevideoinputbox.Text == "")
                {
                    simplevideoinputbox.Text = filename[0];
                    SimpleVideoInputUpdate();
                }
                else
                {
                    QueueApi.SimpleEncodeAudioFileInputCheck(filename[0], out bool isError);
                    if (isError)
                    {
                        ((TextBox)sender).Text = "";
                    }
                }
            }
        }

        /// <summary>
        /// 通用的拖入数据存储
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_Drop(object sender, DragEventArgs e)
        {
            string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (((Border)sender).Name == "videoborder")
            {
                videoinputbox.Text = filenames[0];
                VideoInputUpdate();
            }
            else if (((Border)sender).Name == "audioborder")
            {
                audioinputbox.Text = filenames[0];
                AudioInputUpdate();
            }
            else if (((Border)sender).Name == "muxborder")
            {
                capinputbox.Text = filenames[0];
            }
            else if (((Border)sender).Name == "demuxborder")
            {
                demuxinputbox.Text = filenames[0];
            }
        }

        /// <summary>
        /// 打开 打开文件的面板
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>返回的文件名</returns>
        private string CallOpenFileDialog(string filter = "所有文件(*.*)|*.*")
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = filter;
            if (ofd.ShowDialog() == true)
            {
                return ofd.FileName;
            }
            return "";
        }

        /// <summary>
        /// 打开 保存文件的面板
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        private string CallSaveFileDialog(string filter = "所有文件(*.*)|*.*", string filename = "", string ext = ".h264")
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ext;
            dlg.Filter = filter;
            dlg.FileName = filename;
            if (dlg.ShowDialog() == true)
            {
                return dlg.FileName;
            }
            return "";
        }

        /// <summary>
        /// 打开文件按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileSelect_Click(object sender, RoutedEventArgs e)
        {
            string controlname = ((Button)sender).Name + "box";
            var toTextBox = (TextBox)this.FindName(controlname);
            toTextBox.Text = CallOpenFileDialog();
            if (controlname == "videoinputbox" && videoinputbox.Text != "")
            {
                VideoInputUpdate();
            }
            else if (controlname == "audioinputbox" && audioinputbox.Text != "")
            {
                AudioInputUpdate();
            }
            else if (controlname == "simplevideoinputbox" && simplevideoinputbox.Text != "")
            {
                SimpleVideoInputUpdate();
            }
        }

        /// <summary>
        /// 视频拖入后处理
        /// </summary>
        private void VideoInputUpdate()
        {
            QueueApi.VpyFileInputCheck(videoinputbox.Text, out string cuttextboxText, out string fpstextboxText, out bool cutischeckedIsChecked, out bool isError);
            cuttextbox.Text = cuttextboxText;
            fpstextbox.Text = fpstextboxText;
            cutischecked.IsChecked = cutischeckedIsChecked;
            if (isError)
            {
                videoinputbox.Text = "";
                videooutputbox.Text = "";
            }
            else
            {
                UpdateEncoderSuffix("video", EncoderApi.GetEncoderSuffix("video", videoencoderbox.SelectedIndex));
            }
        }

        /// <summary>
        /// 音频拖入后处理
        /// </summary>
        private void AudioInputUpdate()
        {
            QueueApi.AudioFileInputCheck(audioinputbox.Text, out string audiodelayboxText, out bool isError);
            audiodelaybox.Text = audiodelayboxText;
            UpdateEncoderSuffix("audio", EncoderApi.GetEncoderSuffix("audio", audioencoderbox.SelectedIndex));
        }

        /// <summary>
        /// 简易压制视频拖入后处理
        /// </summary>
        private void SimpleVideoInputUpdate()
        {
            QueueApi.SimpleEncodeFileInputCheck(simplevideoinputbox.Text, out string videoinputboxText, out string audioinputboxtext);
            simplevideoinputbox.Text = videoinputboxText;
            simpleaudioinputbox.Text = audioinputboxtext;
            if (videoinputboxText != "")
            {
                UpdateEncoderSuffix("simpleencode", @"_vsgui." + simplemuxsuffixbox.Text.ToLower());
            }
        }


        /// <summary>
        /// 保存文件按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFileSelect_Click(object sender, RoutedEventArgs e)
        {
            string controlname = ((Button)sender).Name + "box";
            var toTextBox = (TextBox)this.FindName(controlname);
            if (((Button)sender).Tag.ToString() == "videoout")
            {
                toTextBox.Text = CallSaveFileDialog(filename: Path.GetFileNameWithoutExtension(toTextBox.Text), ext: EncoderApi.GetEncoderSuffix("video", videoencoderbox.SelectedIndex));
            }
            else if (((Button)sender).Tag.ToString() == "audioout")
            {
                toTextBox.Text = CallSaveFileDialog(filename: Path.GetFileNameWithoutExtension(toTextBox.Text), ext: EncoderApi.GetEncoderSuffix("audio", videoencoderbox.SelectedIndex));
            }
            else if (((Button)sender).Tag.ToString() == "simplevideoout")
            {
                toTextBox.Text = CallSaveFileDialog(filename: Path.GetFileNameWithoutExtension(toTextBox.Text), ext: simplemuxsuffixbox.Text.ToLower());
            }
        }

        /// <summary>
        /// 编码器设置修改时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void encoderbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).Name == "videoencoderbox")
            {
                if (videoinputbox.Text != "")
                {
                    UpdateEncoderSuffix("video", EncoderApi.GetEncoderSuffix("video", videoencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("videoencoderboxSelectedIndex", videoencoderbox.SelectedIndex.ToString());
            }
            else if (((ComboBox)sender).Name == "simplevideoencoderbox")
            {
                if (simplevideoinputbox.Text != "")
                {
                    UpdateEncoderSuffix("video", EncoderApi.GetEncoderSuffix("video", simplevideoencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("simplevideoencoderboxSelectedIndex", simplevideoencoderbox.SelectedIndex.ToString());
            }
            else if (((ComboBox)sender).Name == "audioencoderbox")
            {
                if (audioinputbox.Text != "")
                {
                    UpdateEncoderSuffix("audio", EncoderApi.GetEncoderSuffix("audio", audioencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("audioencoderboxSelectedIndex", audioencoderbox.SelectedIndex.ToString());
            }
            else if (((ComboBox)sender).Name == "simpleaudioencoderbox")
            {
                if (simpleaudioinputbox.Text != "")
                {
                    UpdateEncoderSuffix("audio", EncoderApi.GetEncoderSuffix("audio", simpleaudioencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("simpleaudioencoderboxSelectedIndex", simpleaudioencoderbox.SelectedIndex.ToString());
            }
        }

        private void UpdateEncoderSuffix(string type, string suffixstr)
        {
            if (type == "video")
            {
                string outputpath = Path.GetDirectoryName(videoinputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(videoinputbox.Text) + suffixstr;
                if (File.Exists(outputpath))
                {
                    outputpath = Path.GetDirectoryName(videoinputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(videoinputbox.Text) + @"-" + DateTime.Now.ToString("yyMMddHHmmss") + suffixstr;
                }
                videooutputbox.Text = outputpath;
            }
            else if (type == "audio")
            {
                string outputpath = Path.GetDirectoryName(audioinputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(audioinputbox.Text) + suffixstr;
                if (File.Exists(outputpath))
                {
                    outputpath = Path.GetDirectoryName(audioinputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(audioinputbox.Text) + @"-" + DateTime.Now.ToString("yyMMddHHmmss") + suffixstr;
                }
                audiooutputbox.Text = outputpath;
            }
            else if (type == "simpleencode")
            {
                string outputpath = Path.GetDirectoryName(simplevideoinputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(simplevideoinputbox.Text) + suffixstr;
                if (File.Exists(outputpath))
                {
                    outputpath = Path.GetDirectoryName(simplevideoinputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(simplevideoinputbox.Text) + @"-" + DateTime.Now.ToString("yyMMddHHmmss") + suffixstr;
                }
                simplevideooutputbox.Text = outputpath;
            }
        }

        /// <summary>
        /// 视频编码按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("videoInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (videooutputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("videoOutputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (videoencoderbox.SelectedIndex == -1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("noEncoderProfilesSelected"), LanguageApi.FindRes("error"));
                return;
            }
            QueueApi.AddQueueList("video", videoencoderbox.SelectedIndex, new string[] { videoinputbox.Text }, videooutputbox.Text);
            UpdateQueueList();
            if (IniApi.IniReadValue("AutoStartQueue") == "true")
            {
                NextQueue(autoStart: true);
            }
        }

        /// <summary>
        /// 音频编码按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AudioSingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("audioInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (audiooutputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("audioOutputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (audioencoderbox.SelectedIndex == -1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("noEncoderProfilesSelected"), LanguageApi.FindRes("error"));
                return;
            }
            if (!Regex.IsMatch(audiodelaybox.Text, @"^-?\d+") || audiodelaybox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("delayFormatError"), LanguageApi.FindRes("error"));
                return;
            }
            if (cutischecked.IsChecked == true && AudioApi.CheckCutStrIsError(cuttextbox.Text))
            {
                MessageBoxApi.Show(LanguageApi.FindRes("cutstrFormatError"), LanguageApi.FindRes("error"));
                return;
            }
            if (cutischecked.IsChecked == true && fpstextbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("fpsstrFormatError"), LanguageApi.FindRes("error"));
                return;
            }

            string cuttext = "";
            if (cutischecked.IsChecked == true) cuttext = cuttextbox.Text;
            QueueApi.AddQueueList("audio", audioencoderbox.SelectedIndex, new string[] { audioinputbox.Text }, audiooutputbox.Text, deletefile: audioinputbox.Text + ".lwi", audiocuttext: cuttext, audiofpstext: fpstextbox.Text, audiodelaytext: audiodelaybox.Text);
            UpdateQueueList();
            if (IniApi.IniReadValue("AutoStartQueue") == "true")
            {
                NextQueue(autoStart: true);
            }
        }

        /// <summary>
        /// 自动编码按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoEncodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("videoInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (videooutputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("videoOutputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (videoencoderbox.SelectedIndex == -1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("noEncoderProfilesSelected"), LanguageApi.FindRes("error"));
                return;
            }
            if (audioinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("audioInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (audiooutputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("audioOutputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (audioencoderbox.SelectedIndex == -1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("noEncoderProfilesSelected"), LanguageApi.FindRes("error"));
                return;
            }
            if (!Regex.IsMatch(audiodelaybox.Text, @"^-?\d+") || audiodelaybox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("delayFormatError"), LanguageApi.FindRes("error"));
                return;
            }
            if (capinputbox.Text != "")
            {
                if (!File.Exists(capinputbox.Text))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFileDoesNotExist"), LanguageApi.FindRes("error"));
                    return;
                }
                if (!ChapterApi.ChapterFormatCheck(capinputbox.Text))
                {
                    return;
                }
            }
            //封装格式选择
            //生成groud名
            string groupname = CommonApi.GetNewSeed();
            QueueApi.AddQueueList("video", videoencoderbox.SelectedIndex, new string[] { videoinputbox.Text }, videooutputbox.Text, group: groupname);
            string cuttext = "";
            if (cutischecked.IsChecked == true) cuttext = cuttextbox.Text;
            QueueApi.AddQueueList("audio", audioencoderbox.SelectedIndex, new string[] { audioinputbox.Text }, audiooutputbox.Text, deletefile: audioinputbox.Text + ".lwi", audiocuttext: cuttext, audiofpstext: fpstextbox.Text, audiodelaytext: audiodelaybox.Text, group: groupname);
            //再添加一个混流任务
            QueueApi.AddQueueList("mux", 0, new string[] { videooutputbox.Text, audiooutputbox.Text, capinputbox.Text }, Path.GetDirectoryName(videooutputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(videooutputbox.Text) + @"_mux." + muxsuffixbox.Text.ToLower(), deletefile: videooutputbox.Text + "|" + audiooutputbox.Text, group: groupname);
            UpdateQueueList();
            if (IniApi.IniReadValue("AutoStartQueue") == "true")
            {
                NextQueue(autoStart: true);
            }
        }

        private void SimpleAutoEncodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (simplevideoinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("videoInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (simplevideooutputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("videoOutputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (simplevideoencoderbox.SelectedIndex == -1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("noEncoderProfilesSelected"), LanguageApi.FindRes("error"));
                return;
            }
            if (simpleaudioinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("audioInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (simpleaudioencoderbox.SelectedIndex == -1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("noEncoderProfilesSelected"), LanguageApi.FindRes("error"));
                return;
            }
            if (simplecapinputbox.Text != "")
            {
                if (!File.Exists(simplecapinputbox.Text))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFileDoesNotExist"), LanguageApi.FindRes("error"));
                    return;
                }
                if (!ChapterApi.ChapterFormatCheck(simplecapinputbox.Text))
                {
                    return;
                }
            }
            if (simpleresolutionbox.SelectedIndex != 0 && Regex.Matches(simpleresolutionbox.Text.ToUpper(), @"\d+P").Count < 1)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("resolutionFormatError"), LanguageApi.FindRes("error"));
                return;
            }
            if (simpleasspathinputbox.Text != "")
            {
                if (!File.Exists(simpleasspathinputbox.Text))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("subtitleFileError"), LanguageApi.FindRes("error"));
                    return;
                }
            }

            //生成groud名
            string groupname = CommonApi.GetNewSeed();
            string tempvideopath = Path.GetDirectoryName(simplevideooutputbox.Text) + @"\" + groupname + "_v" + EncoderApi.GetEncoderSuffix("video", simplevideoencoderbox.SelectedIndex);
            string tempaudiopath = Path.GetDirectoryName(simplevideooutputbox.Text) + @"\" + groupname + "_a" + EncoderApi.GetEncoderSuffix("audio", simpleaudioencoderbox.SelectedIndex);
            QueueApi.AddQueueList("video", simplevideoencoderbox.SelectedIndex, new string[] { simplevideoinputbox.Text }, tempvideopath, resolution: simpleresolutionbox.Text.ToUpper(), subtitle: simpleasspathinputbox.Text, group: groupname);
            QueueApi.AddQueueList("audio", simpleaudioencoderbox.SelectedIndex, new string[] { simpleaudioinputbox.Text }, tempaudiopath, deletefile: simpleaudioinputbox.Text + ".lwi", group: groupname);
            //再添加一个混流任务
            QueueApi.AddQueueList("mux", 0, new string[] { tempvideopath, tempaudiopath, simplecapinputbox.Text }, Path.GetDirectoryName(simplevideooutputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(simplevideooutputbox.Text) + @"_mux." + simplemuxsuffixbox.Text.ToLower(), deletefile: tempvideopath + "|" + tempaudiopath, group: groupname);
            UpdateQueueList();
            if (IniApi.IniReadValue("AutoStartQueue") == "true")
            {
                NextQueue(autoStart: true);
            }
        }

        /// <summary>
        /// 队列开始后自动下一个任务
        /// </summary>
        private void NextQueue(int index = 0, bool autoStart = false)
        {
            var queuelist = QueueApi.GetQueueList();
            //自动启动，有任一任务进行中就退出
            if (autoStart)
            {
                for (int i = 0; i < queuelist.Count; i++)
                {
                    if (queuelist[i]["status"].ToString() == "running")
                    {
                        return;
                    }
                }
            }
            //完成任务后的自动下一队列
            for (int i = index; i < queuelist.Count; i++)
            {
                if (queuelist[i]["status"].ToString() == "waiting")
                {
                    if (queuelist[i]["type"].ToString() == "mux")
                    {
                        if (!QueueApi.CheckGroupMuxJobIsReady(queuelist[i]["queueid"].ToString()))
                        {
                            continue;
                        }
                    }
                    StartQueueJob(queuelist[i]["queueid"].ToString());
                    break;
                }
            }
        }

        internal void StartQueueJob(string queueid, bool isSingle = false)
        {
            new Thread(
                () =>
                {
                    QueueApi.SetQueueListitem(queueid, "status", "running");
                    QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("preparing"));
                    QueueApi.SetQueueListitem(queueid, "starttime", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString());
                    UpdateQueueList();

                    //特殊格式文件特殊处理
                    QueueApi.SpecialFormatPreProcess(queueid);
                    //判断是否需要生成文件
                    QueueApi.MakeScriptFile(queueid);
                    //写入totalframes
                    QueueApi.UpdateTotalframes(queueid);

                    string clipath = QueueApi.GetQueueListitem(queueid, "clipath");
                    if (QueueApi.GetQueueListitem(queueid, "type") == "video" && IniApi.IniReadValue("UseSystemEnvironment") == "true")
                    {
                        clipath = "";
                    }
                    ProcessApi.RunProcess(clipath, QueueApi.GetQueueListitem(queueid, "command"), DataReceived, Exited, Pided);



                    void DataReceived(string data, bool processIsExited)
                    {
                        if (!string.IsNullOrEmpty(data) && !processIsExited)
                        {
                            UpdateLogBox();
                            if ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - QueueApi.lastUpdateTime) < 1000)
                            {
                                return;
                            }
                            QueueApi.lastUpdateTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                            QueueApi.UpdateProgressStatus(queueid, data);
                            UpdateQueueList();
                        }
                    }
                    void Exited()
                    {
                        QueueApi.DoWhenProcessFinish(queueid);
                        if (!isSingle && !forcedStop)
                        {
                            NextQueue(QueueApi.FindQueueListitemIndexFromQueueid(queueid));
                        }
                        UpdateQueueList();
                    }
                    void Pided(string pid)
                    {
                        QueueApi.SetQueueListitem(queueid, "processTheadId", pid);
                    }
                }
            ).Start();

        }

        /// <summary>
        /// 队列按钮的变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueueListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateQueueListSelectionButton();
        }

        /// <summary>
        /// 更新选中按钮
        /// </summary>
        private void UpdateQueueListSelectionButton()
        {
            if (QueueListView.SelectedIndex >= 0)
            {
                string selectQueueid = QueueApi.GetQueueListitemFromSelectedIndex(QueueListView.SelectedIndex, "queueid");
                string caseStr = QueueApi.GetQueueListitem(selectQueueid, "status");
                switch (caseStr)
                {
                    case "running":
                        resetQueueSelectButton();
                        PauseQueueItem.Visibility = Visibility.Visible;
                        StopQueueItem.Visibility = Visibility.Visible;
                        break;
                    case "waiting":
                        resetQueueSelectButton();
                        StartQueueItem.Visibility = Visibility.Visible;
                        if (QueueApi.GetQueueListitem(selectQueueid, "group") != "")
                        {
                            if (QueueApi.GetQueueListitem(selectQueueid, "type") == "mux")
                            {
                                StartQueueItem.IsEnabled = false;
                                if (QueueApi.CheckGroupMuxJobIsReady(selectQueueid))
                                {
                                    StartQueueItem.IsEnabled = true;
                                }
                            }
                        }
                        DeleteQueueItem.Visibility = Visibility.Visible;
                        break;
                    case "pause":
                        resetQueueSelectButton();
                        ResumeQueueItem.Visibility = Visibility.Visible;
                        break;
                    case "error":
                        resetQueueSelectButton();
                        ResetQueueItem.Visibility = Visibility.Visible;
                        DeleteQueueItem.Visibility = Visibility.Visible;
                        break;
                    case "stop":
                        resetQueueSelectButton();
                        ResetQueueItem.Visibility = Visibility.Visible;
                        DeleteQueueItem.Visibility = Visibility.Visible;
                        break;
                    case "finish":
                        resetQueueSelectButton();
                        ResetQueueItem.Visibility = Visibility.Visible;
                        DeleteQueueItem.Visibility = Visibility.Visible;
                        break;
                    default:
                        resetQueueSelectButton();
                        break;
                }
                //组任务如果有其一正在运行，则不允许删除
                if (QueueApi.GetQueueListitem(selectQueueid, "group") != "")
                {
                    if (!QueueApi.CheckGroupJobCanDelete(selectQueueid))
                    {
                        DeleteQueueItem.IsEnabled = false;
                    }
                }
            }
            else
            {
                resetQueueSelectButton();
            }

            void resetQueueSelectButton()
            {
                StartQueueItem.Visibility = Visibility.Collapsed;
                StartQueueItem.IsEnabled = true;
                PauseQueueItem.Visibility = Visibility.Collapsed;
                ResumeQueueItem.Visibility = Visibility.Collapsed;
                StopQueueItem.Visibility = Visibility.Collapsed;
                ResetQueueItem.Visibility = Visibility.Collapsed;
                DeleteQueueItem.Visibility = Visibility.Collapsed;
                DeleteQueueItem.IsEnabled = true;
            }
        }

        /// <summary>
        /// 停止所有
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopQueueAll_Click(object sender, RoutedEventArgs e)
        {
            forcedStop = true;
            ProcessApi.StopProcessAll();
        }

        /// <summary>
        /// 开始队列按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartQueueAll_Click(object sender, RoutedEventArgs e)
        {
            forcedStop = false;
            NextQueue();
        }

        /// <summary>
        /// 单独开始选中的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartQueueItem_Click(object sender, RoutedEventArgs e)
        {
            forcedStop = false;
            string startQueueid = QueueApi.GetQueueListitemFromSelectedIndex(QueueListView.SelectedIndex, "queueid");
            StartQueueJob(startQueueid, true);
            UpdateQueueList();
        }

        /// <summary>
        /// 暂停选中的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseQueueItem_Click(object sender, RoutedEventArgs e)
        {
            string pauseQueueid = QueueApi.GetQueueListitemFromSelectedIndex(QueueListView.SelectedIndex, "queueid");
            ProcessApi.PauseProcessItem(pauseQueueid);
            UpdateQueueList();
        }

        /// <summary>
        /// 继续选中的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResumeQueueItem_Click(object sender, RoutedEventArgs e)
        {
            string resumeQueueid = QueueApi.GetQueueListitemFromSelectedIndex(QueueListView.SelectedIndex, "queueid");
            ProcessApi.ResumeProcessItem(resumeQueueid);
            UpdateQueueList();
        }

        /// <summary>
        /// 停止选中的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopQueueItem_Click(object sender, RoutedEventArgs e)
        {
            forcedStop = true;
            string stopQueueid = QueueApi.GetQueueListitemFromSelectedIndex(QueueListView.SelectedIndex, "queueid");
            ProcessApi.StopProcessItem(stopQueueid);
            UpdateQueueList();
        }

        /// <summary>
        /// 重置选中的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetQueueItem_Click(object sender, RoutedEventArgs e)
        {
            string resetQueueid = QueueApi.GetQueueListitemFromSelectedIndex(QueueListView.SelectedIndex, "queueid");
            QueueApi.ResetQueueItem(resetQueueid);
            UpdateQueueList();
            QueueApi.SaveQueueList();
        }

        /// <summary>
        /// 删除选中的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteQueueItem_Click(object sender, RoutedEventArgs e)
        {
            string removeQueueid = QueueApi.GetQueueListitemFromSelectedIndex(QueueListView.SelectedIndex, "queueid");
            QueueApi.DeleteQueueItem(removeQueueid);
            UpdateQueueList();
            QueueApi.SaveQueueList();
        }

        /// <summary>
        /// 剪切按钮切换时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cutischecked_Checked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                cutpanel.Visibility = Visibility.Visible;
            }
            else
            {
                cutpanel.Visibility = Visibility.Collapsed;
            }
        }


        /// <summary>
        /// 解流按钮点击时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DemuxStartButton_Click(object sender, RoutedEventArgs e)
        {
            demuxInfoTextBox.Text = LanguageApi.FindRes("demuxing");
            demuxStartButton.IsEnabled = false;
            DemuxApi.StartDemux(demuxinputbox.Text, ((ComboBoxItem)demuxclibox.SelectedValue).Content.ToString(), WhenDataReceived, WhenExited);
            void WhenDataReceived(string message)
            {
                Dispatcher.Invoke(() =>
                {
                    demuxInfoTextBox.Text = message;
                });
            }
            void WhenExited(string message)
            {
                Dispatcher.Invoke(() =>
                {
                    demuxInfoTextBox.Text = message;
                    demuxStartButton.IsEnabled = true;
                });
            }
        }

        /// <summary>
        /// 简易混流按钮点击时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SmuxStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (smuxvideoinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("videoInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (smuxaudioinputbox.Text == "")
            {
                MessageBoxApi.Show(LanguageApi.FindRes("audioInputIsEmpty"), LanguageApi.FindRes("error"));
                return;
            }
            if (smuxchapterinputbox.Text != "")
            {
                if (!File.Exists(smuxchapterinputbox.Text))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFileDoesNotExist"), LanguageApi.FindRes("error"));
                    return;
                }
                if (!ChapterApi.ChapterFormatCheck(smuxchapterinputbox.Text))
                {
                    return;
                }
            }

            string[] inputlist;
            if (smuxchapterinputbox.Text != "")
            {
                inputlist = new string[] { smuxvideoinputbox.Text, smuxaudioinputbox.Text, smuxchapterinputbox.Text };
            }
            else
            {
                inputlist = new string[] { smuxvideoinputbox.Text, smuxaudioinputbox.Text };
            }
            smuxStartButton.IsEnabled = false;
            MuxApi.StartSMux(inputlist, smuxsuffixbox.Text.ToLower(), WhenDataReceived, WhenExited);
            void WhenDataReceived(string message)
            {
                Dispatcher.Invoke(() =>
                {
                    smuxinfotext.Text = message;
                });
            }
            void WhenExited(string message)
            {
                Dispatcher.Invoke(() =>
                {
                    smuxinfotext.Text = message;
                    smuxStartButton.IsEnabled = true;
                });
            }
        }

        /// <summary>
        /// 设置选项切换后
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                IniApi.IniWriteValue(((CheckBox)sender).Name.ToString(), "true");
                //EnableQueueLog特殊处理
                if (((CheckBox)sender).Name.ToString() == "EnableQueueLog")
                {
                    logTextbox.Visibility = Visibility.Visible;
                }
            }
            else
            {
                IniApi.IniWriteValue(((CheckBox)sender).Name.ToString(), "false");
                //EnableQueueLog特殊处理
                if (((CheckBox)sender).Name.ToString() == "EnableQueueLog")
                {
                    logTextbox.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 软件关闭前检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (QueueApi.runningQueueCount > 0)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("exitQueueIsRuning"), LanguageApi.FindRes("error"));
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 退出后保护
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (QueueApi.runningQueueCount > 0)
            {
                forcedStop = true;
                ProcessApi.StopProcessAll();
            }
        }

        private void muxsuffixbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IniApi.IniWriteValue("muxsuffixbox", ((ComboBox)sender).SelectedIndex.ToString());
            if (((ComboBox)sender).Name == "simplemuxsuffixbox")
            {
                if (simplevideooutputbox.Text != "")
                {
                    try
                    {
                        string path = simplevideooutputbox.Text;
                        string suffix = ((ComboBoxItem)simplemuxsuffixbox.SelectedValue).Content.ToString();
                        simplevideooutputbox.Text = Path.GetDirectoryName(path) + @"\" + Path.GetFileNameWithoutExtension(path) + "." + suffix;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 打开编辑器按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenEditorButton_Click(object sender, RoutedEventArgs e)
        {
            if (IniApi.IniReadValue("InstallVseditPrompted") != "true")
            {
                if (CommonApi.CheckVSEditorInstall() < 2)
                {
                    var messageboxresult = MessageBoxApi.Show(LanguageApi.FindRes("vpyEditorAssociationTipsDesc"), LanguageApi.FindRes("tips"), MessageWindow.MessageBoxButton.YesNoNomore);
                    if (messageboxresult == MessageWindow.MessageResult.Yes)
                    {
                        installVsEditorDo();
                    }
                    else if (messageboxresult == MessageWindow.MessageResult.NoMore)
                    {
                        IniApi.IniWriteValue("InstallVseditPrompted", "true");
                    }
                }
            }
            if (videoinputbox.Text != "" && Path.GetExtension(videoinputbox.Text).ToLower() == ".vpy")
            {
                Process.Start(binpath + @"\vs\vsedit.exe", videoinputbox.Text);
            }
            else
            {
                Process.Start(binpath + @"\vs\vsedit.exe");
            }
        }

        //保存编码器在线按钮
        private void NetEncoderJsonBoxSave_Click(object sender, RoutedEventArgs e)
        {
            UseNetEncoderJsonDesc.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            UseNetEncoderJsonDesc.Text = LanguageApi.FindRes("netEncoderUpdating");
            IniApi.IniWriteValue("UseNetEncoderJsonBox", UseNetEncoderJsonBox.Text);
            ReCheckEncoderProfiles();
        }

        private void ReScanLocalFile_Click(object sender, RoutedEventArgs e)
        {
            ReScanButton.IsEnabled = false;
            CommonApi.TryDeleteFile(binpath + @"\json\version.json");
            UpdateApi.UpdateCheck(UpdateProgressCall);
        }

        private void UpdateProgressCall(string message)
        {
            Dispatcher.Invoke(() =>
            {
                updateinfotext.Text = message;
            });
        }

        private void InstallVseditorButton_Click(object sender, RoutedEventArgs e)
        {
            installVsEditorDo();
        }

        private void installVsEditorDo()
        {
            string cmdstr = "@echo off\r\n>nul 2>&1 \"%SYSTEMROOT%\\system32\\cacls.exe\" \"%SYSTEMROOT%\\system32\\config\\system\"\r\nif '%errorlevel%' NEQ '0' (\r\ngoto UACPrompt\r\n) else ( goto gotAdmin )\r\n:UACPrompt\r\necho Set UAC = CreateObject^(\"Shell.Application\"^) > \"%temp%\\getadmin.vbs\"\r\necho UAC.ShellExecute \"%~s0\", \"\", \"\", \"runas\", 1 >> \"%temp%\\getadmin.vbs\"\r\n\"%temp%\\getadmin.vbs\"\r\nexit /B\r\n:gotAdmin\r\nset local=%~dp0\r\nset localexe=\"\\\"%local%vsedit.exe\\\" \\\"%%1\\\"\"\r\necho ※Please run as admin\r\necho.\r\necho set vpy open mode\r\nreg add \"HKEY_CLASSES_ROOT\\.vpy\" /v \"\" /d \"vpy_auto_file\" /f\r\necho.\r\necho set vsedit path\r\nreg add \"HKEY_CLASSES_ROOT\\vpy_auto_file\\shell\\open\\command\" /v \"\" /d %localexe% /f\r\necho.";
            File.WriteAllText(binpath + @"\vs\installvseditor.bat", cmdstr);
            Process.Start(binpath + @"\vs\installvseditor.bat");
            Thread.Sleep(1000);

            var messageboxresult = MessageBoxApi.Show(LanguageApi.FindRes("p001"), LanguageApi.FindRes("tips"), MessageWindow.MessageBoxButton.YesNo);
            if (messageboxresult == MessageWindow.MessageResult.Yes)
            {
                CommonApi.KillProcess("explorer");
                Process.Start("explorer.exe");
            }
            CommonApi.TryDeleteFile(binpath + @"\vs\installvseditor.bat");
            UpdateVseditorButtonStatus();
        }

        private void UnInstallVseditorButton_Click(object sender, RoutedEventArgs e)
        {
            string cmdstr = "@echo off\r\n>nul 2>&1 \"%SYSTEMROOT%\\system32\\cacls.exe\" \"%SYSTEMROOT%\\system32\\config\\system\"\r\nif '%errorlevel%' NEQ '0' (\r\ngoto UACPrompt\r\n) else ( goto gotAdmin )\r\n:UACPrompt\r\necho Set UAC = CreateObject^(\"Shell.Application\"^) > \"%temp%\\getadmin.vbs\"\r\necho UAC.ShellExecute \"%~s0\", \"\", \"\", \"runas\", 1 >> \"%temp%\\getadmin.vbs\"\r\n\"%temp%\\getadmin.vbs\"\r\nexit /B\r\n:gotAdmin\r\nset local=%~dp0\r\nset localexe=\"\\\"%local%vsedit.exe\\\" \\\"%%1\\\"\"\r\necho ※Please run as admin\r\necho.\r\necho del vpy openmode\r\nreg delete \"HKEY_CLASSES_ROOT\\.vpy\" /f\r\necho.\r\necho del vsedit path\r\nreg delete \"HKEY_CLASSES_ROOT\\vpy_auto_file\" /f\r\necho.";
            File.WriteAllText(binpath + @"\vs\installvseditor-un.bat", cmdstr);
            Process.Start(binpath + @"\vs\installvseditor-un.bat");
            Thread.Sleep(1000);
           // Process.Start("explorer.exe");
            CommonApi.TryDeleteFile(binpath + @"\vs\installvseditor-un.bat");
            UpdateVseditorButtonStatus();
        }

        private void UpdateVseditorButtonStatus()
        {
            if (CommonApi.CheckVSEditorInstall() == 2)
            {
                installvseditorbutton.Visibility = Visibility.Collapsed;
                uninstallvseditorbutton.Visibility = Visibility.Visible;
                associatedVpyPanel.Visibility = Visibility.Visible;
            }
            else
            {
                installvseditorbutton.Visibility = Visibility.Visible;
                uninstallvseditorbutton.Visibility = Visibility.Collapsed;
                associatedVpyPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateVersion()
        {
            new Thread(
                () =>
                {
                    string[] versionList = VersionApi.GetVersion();
                    bool isSysEnvironmentReady = false;
                    if (versionList[2] != null && versionList[3] != null)
                    {
                        isSysEnvironmentReady = true;
                    }
                    Dispatcher.Invoke(() =>
                    {
                        if (isSysEnvironmentReady == false)
                        {
                            UseSystemEnvironment.IsEnabled = false;
                            UseSystemEnvironment.IsChecked = false;
                        }
                        if (CommonApi.CheckVSEditorInstall() == 2 && IniApi.IniReadValue("UseSystemEnvironment") == "true")
                        {
                            MessageBoxApi.Show(LanguageApi.FindRes("useSystemEnvironmentWarningVsEditor"), LanguageApi.FindRes("tips"));
                        }
                        buildinpyvertext.Text = versionList[0];
                        if (versionList[0] == null) buildinpyvertext.Text = LanguageApi.FindRes("notInstalled");
                        buildinvsvertext.Text = versionList[1];
                        if (versionList[1] == null) buildinvsvertext.Text = LanguageApi.FindRes("notInstalled");
                        systempyvertext.Text = versionList[2];
                        if (versionList[2] == null) systempyvertext.Text = LanguageApi.FindRes("notInstalled");
                        systemvsvertext.Text = versionList[3];
                        if (versionList[3] == null) systemvsvertext.Text = LanguageApi.FindRes("notInstalled");
                    });
                }
                ).Start();
        }

        private void StartVsrepoButton_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = binpath + @"\vs\VSRepoGUI.exe";
            startInfo.WorkingDirectory = binpath + @"\vs\";
            Process.Start(startInfo);
            //Process.Start(binpath + @"\vs\VSRepoGUI.exe");
        }

        private void MkvtoolnixButton_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = binpath + @"\tools\mkvtoolnix\mkvtoolnix-gui.exe";
            startInfo.WorkingDirectory = binpath + @"\tools\mkvtoolnix\";
            Process.Start(startInfo);
            //Process.Start(binpath + @"\tools\mkvtoolnix\mkvtoolnix-gui.exe");
        }

        /// <summary>
        /// 点击视频编码器编辑按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoEncoderButton_Click(object sender, RoutedEventArgs e)
        {
            EncoderWindow ww = new EncoderWindow("video");
            ww.Owner = this;
            ww.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ww.ShowDialog();
            UpdateEncoderProfiles();
        }

        /// <summary>
        /// 点击音频编码器编辑按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AudioEncoderButton_Click(object sender, RoutedEventArgs e)
        {
            EncoderWindow ww = new EncoderWindow("audio");
            ww.Owner = this;
            ww.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ww.ShowDialog();
            UpdateEncoderProfiles();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectLanguage = "English";
            if (LanguageComboBox.SelectedItem != null)
            {
                selectLanguage = LanguageComboBox.SelectedItem.ToString();
            }
            LanguageApi.SwitchLanguage(selectLanguage);

        }

        private void UpdateLogBox()
        {
            Dispatcher.Invoke(() =>
            {
                var loglist = logBoxStr.Split("\r\n");
                Console.WriteLine(loglist.Length);
                if (loglist.Length > 50)
                {
                    string newlogBoxStr = "";
                    for (int i = loglist.Length - 50; i < loglist.Length; i++)
                    {
                        newlogBoxStr += "\r\n" + loglist[i];
                    }
                    logBoxStr = newlogBoxStr;
                }
                logTextbox.Text = logBoxStr;
                //logTextbox.ScrollToLine(50);
                logTextbox.ScrollToVerticalOffset(99999);
                //logTextbox.ScrollToEnd();
            });
        }

        /// <summary>
        /// 压制页签切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EncodeTabButton_Checked(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).Name == "simpleEncodeButton")
            {
                simpleEncodePanel.Visibility = Visibility.Visible;
                advancedEncodePanel.Visibility = Visibility.Collapsed;
                IniApi.IniWriteValue("encodeTab", "simple");
            }
            else
            {
                simpleEncodePanel.Visibility = Visibility.Collapsed;
                advancedEncodePanel.Visibility = Visibility.Visible;
                IniApi.IniWriteValue("encodeTab", "advanced");
            }
        }

        private void SimpleOpenEditorButton_Click(object sender, RoutedEventArgs e)
        {
            string script = VideoApi.MakeVideoScript(simplevideoinputbox.Text, simpleresolutionbox.Text.ToUpper(), simpleasspathinputbox.Text);
            if (script == null)
            {
                MessageBoxApi.Show("DEBUG ERROR: 视频信息读取错误，麻烦提供视频文件协助调试", LanguageApi.FindRes("error"));
                script = "";
            }
            VideoApi.PreviewTempVpy(script);
        }

        private void VSGUITextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer", "https://github.com/YohoYang/VSGUI");
        }

        private void SBSUBTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer", "https://www.sbsub.com/");
        }

        private void OpenFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(binpath + @"\vs\vapoursynth64\plugins");
            System.Diagnostics.Process.Start("explorer.exe", binpath + @"\vs\vapoursynth64\plugins");
        }
        private void OpenScriptsButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(binpath + @"\vs\python\libs");
            System.Diagnostics.Process.Start("explorer.exe", binpath + @"\vs\python\libs");
        }

    }
}
