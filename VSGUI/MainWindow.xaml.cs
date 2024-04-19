using HandyControl.Data;
using Microsoft.Win32;
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
using System.Windows.Media;
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
        public static string envpath = null;
        private bool forcedStop = false;
        private string coreversion = "v1.0.10";
        public static string logBoxStr = "";
        private string[] videoMultiInputLists, audioMultiInputLists;

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
            string proxyurl = "";
            if (IniApi.IniReadValue("proxyurl") != "")
            {
                proxyurl = IniApi.IniReadValue("proxyurl");
            }
            UpdateApi.UpdateCheck(proxyurl, UpdateProgressCall, UpdateFinishCall);

            //显示队列
            UpdateQueueList();

            //恢复设置的状态
            ResumeConfig();

            //读取编码器配置
            ReCheckEncoderProfiles();

            //更新vseditor关联状态
            UpdateVseditorButtonStatus();

            //获取py和vs版本的情况
            UpdateEnvVersion();
        }

        /// <summary>
        /// 读取编码器配置
        /// </summary>
        private void ReCheckEncoderProfiles()
        {
            UpdateEncoderProfiles();
            string encoderJsonUrl = UseNetEncoderJsonBox.Text;
            string proxy = proxyUrl.Text;
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

                     },
                     proxy);
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
            //代理设置
            if (IniApi.IniReadValue("proxyurl") != "")
            {
                proxyUrl.Text = IniApi.IniReadValue("proxyurl");
            }
            //xyvsfilter开关
            if (IniApi.IniReadValue("vsfiltermodEnable") == "true")
            {
                this.vsfiltermodCheckBox.IsChecked = true;
            }
            // 环境选项恢复移动到环境检测后
        }

        /// <summary>
        /// 从json更新编码器预置参数
        /// </summary>
        private void UpdateEncoderProfiles()
        {
            Dispatcher.Invoke(() =>
            {
                videoencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(EncoderApi.GetEncoderJson(), "video");
                simplevideoencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(EncoderApi.GetEncoderJson(), "video");
                audioencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(EncoderApi.GetEncoderJson(), "audio");
                simpleaudioencoderbox.ItemsSource = EncoderApi.GetEncoderProfiles(EncoderApi.GetEncoderJson(), "audio");
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

        public void UpdateQueueList(bool notBtn = false)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    int lastSelected = QueueListView.SelectedIndex;
                    var queueItemData = QueueApi.GetQueueMember();
                    //更新右侧按钮状态
                    if (notBtn == false)
                    {
                        if (QueueApi.runningQueueCount > 0)
                        {
                            StartQueueAll.IsEnabled = false;
                            StopQueueAll.IsEnabled = true;
                            StartQueueAll.Visibility = Visibility.Collapsed;
                            StopQueueAll.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            StartQueueAll.IsEnabled = true;
                            StopQueueAll.IsEnabled = false;
                            StartQueueAll.Visibility = Visibility.Visible;
                            StopQueueAll.Visibility = Visibility.Collapsed;
                        }
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
            if (((TextBox)sender).Name == "simpleasspathinputbox")
            {
                string localstr = "";
                if (this.simpleasspathinputbox.Text != "")
                {
                    localstr = this.simpleasspathinputbox.Text;
                }

                if (filename.Length > 1)
                {
                    if (localstr != "")
                    {
                        localstr += "|";
                    }
                    string assinputStr = "";
                    for (int k = 0; k < filename.Length; k++)
                    {
                        assinputStr += filename[k];
                        if (k != filename.Length - 1)
                        {
                            assinputStr += "|";
                        }
                    }

                    localstr += assinputStr;
                }
                else
                {
                    if (localstr != "")
                    {
                        localstr += "|";
                    }
                    localstr += filename[0];
                }
                this.simpleasspathinputbox.Text = localstr;
                return;
            }
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
            if (filenames != null)
            {
                if (((Border)sender).Name == "videoborder")
                {
                    if (filenames.Length > 1)
                    {
                        MultiInputUpdate(1, filenames);
                    }
                    else
                    {
                        MultiInputClear(1);
                        videoinputbox.Text = filenames[0];
                        VideoInputUpdate();
                    }
                }
                else if (((Border)sender).Name == "audioborder")
                {
                    if (filenames.Length > 1)
                    {
                        MultiInputUpdate(2, filenames);
                    }
                    else
                    {
                        MultiInputClear(2);
                        audioinputbox.Text = filenames[0];
                        AudioInputUpdate();
                    }
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
        }

        /// <summary>
        /// 打开 打开文件的面板
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>返回的文件名</returns>
        private string[] CallOpenFileDialog(string filter = "所有文件(*.*)|*.*", bool isMulti = false)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            if (isMulti)
            {
                ofd.Multiselect = true;
            }
            ofd.Filter = filter;
            if (ofd.ShowDialog() == true)
            {
                string[] rStrs = new string[ofd.FileNames.Length];
                int index = 0;
                foreach (string filename in ofd.FileNames)
                {
                    rStrs[index] = filename;
                    index++;
                }
                return rStrs;
            }
            return null;
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
            string[] files = CallOpenFileDialog();
            if (files != null)
            {
                toTextBox.Text = files[0];
            }
            if (controlname == "simplevideoinputbox" && simplevideoinputbox.Text != "")
            {
                SimpleVideoInputUpdate();
            }
        }

        /// <summary>
        /// 打开文件按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenMultiFileSelect_Click(object sender, RoutedEventArgs e)
        {
            string controlname = ((Button)sender).Name + "box";
            var toTextBox = (TextBox)this.FindName(controlname);
            string[] files = CallOpenFileDialog(isMulti: true);
            if (files == null)
            {
                return;
            }
            if (controlname == "simpleasspathinputbox")
            {
                string localstr = "";
                if (this.simpleasspathinputbox.Text != "")
                {
                    localstr = this.simpleasspathinputbox.Text;
                }

                if (files.Length > 1)
                {
                    if (localstr != "")
                    {
                        localstr += "|";
                    }
                    string assinputStr = "";
                    for (int k = 0; k < files.Length; k++)
                    {
                        assinputStr += files[k];
                        if (k != files.Length - 1)
                        {
                            assinputStr += "|";
                        }
                    }

                    localstr += assinputStr;
                }
                else
                {
                    if (localstr != "")
                    {
                        localstr += "|";
                    }
                    localstr += files[0];
                }
                this.simpleasspathinputbox.Text = localstr;
                return;
            }
            if (files.Length > 1)
            {
                //多文件输入
                if (controlname == "videoinputbox")
                {
                    MultiInputUpdate(1, files);
                }
                else if (controlname == "audioinputbox")
                {
                    MultiInputUpdate(2, files);
                }
            }
            else
            {
                if (controlname == "videoinputbox")
                {
                    MultiInputClear(1);
                    toTextBox.Text = files[0];
                    if (videoinputbox.Text != "")
                    {
                        VideoInputUpdate();
                    }
                }
                else if (controlname == "audioinputbox")
                {
                    MultiInputClear(2);
                    toTextBox.Text = files[0];
                    if (audioinputbox.Text != "")
                    {
                        AudioInputUpdate();
                    }
                }
            }
        }

        /// <summary>
        /// Python 环境按钮选择
        /// </summary>
        private void SelectCustomEnvPath_Click(object sender, RoutedEventArgs e)
        {
            string[] files = CallOpenFileDialog("Python Executable|python.exe");
            if (files != null)
            {
                IniApi.IniWriteValue("customEnvPath", System.IO.Path.GetDirectoryName(files[0]) + @"\");
                UpdateEnvVersion();
            }
        }


        /// <summary>
        /// 视频拖入后处理
        /// </summary>
        private void VideoInputUpdate()
        {
            string inputStr = videoinputbox.Text;
            UpdateEncoderSuffix("video", inputStr, EncoderApi.GetEncoderSuffix("video", videoencoderbox.SelectedIndex));
            this.videoinputPbSucc.Visibility = Visibility.Collapsed;
            this.videoinputPb.Visibility = Visibility.Visible;
            new Thread(
                () =>
                {
                    QueueApi.VpyFileInputCheck(inputStr, out string cuttextboxText, out string fpstextboxText, out bool cutischeckedIsChecked, out bool isError);
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            cuttextbox.Text = cuttextboxText;
                            fpstextbox.Text = fpstextboxText;
                            cutischecked.IsChecked = cutischeckedIsChecked;
                            this.videoinputPb.Visibility = Visibility.Collapsed;
                            if (isError)
                            {
                                videoinputbox.Text = "";
                                videooutputbox.Text = "";
                            }
                            else
                            {
                                this.videoinputPbSucc.Visibility = Visibility.Visible;
                            }
                        });
                    }
                    catch (Exception)
                    {
                        //执行时可能遇到问题
                    }
                }
            ).Start();



        }


        /// <summary>
        /// 音频拖入后处理
        /// </summary>
        private void AudioInputUpdate()
        {
            string inputStr = audioinputbox.Text;
            UpdateEncoderSuffix("audio", inputStr, EncoderApi.GetEncoderSuffix("audio", audioencoderbox.SelectedIndex));
            this.audioinputPbSucc.Visibility = Visibility.Collapsed;
            this.audioinputPb.Visibility = Visibility.Visible;
            new Thread(
                () =>
                {
                    QueueApi.AudioFileInputCheck(inputStr, out string audiodelayboxText, out bool isError);
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            this.audioinputPb.Visibility = Visibility.Collapsed;
                            audiodelaybox.Text = audiodelayboxText;
                            if (!isError)
                            {
                                this.audioinputPbSucc.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                if (!(Path.GetExtension(this.audioinputbox.Text) == ".avs"))
                                {
                                    this.audioinputbox.Text = "";
                                    this.audiooutputbox.Text = "";
                                }
                            }
                        });
                    }
                    catch (Exception)
                    {
                        //执行时可能遇到问题
                    }
                }
            ).Start();
        }

        /// <summary>
        /// 多文件输入时的处理
        /// </summary>
        private void MultiInputUpdate(int type, string[] inputList)
        {
            //type 1 视频 type 2 音频
            if (type == 1)
            {
                videoMultiInputLists = inputList;
                this.videoinputbox.Text = inputList.Length + LanguageApi.FindRes("p025");
                this.videooutputbox.Text = inputList.Length + LanguageApi.FindRes("p026");
                this.videooutputbox.IsEnabled = false;
                this.videooutput.IsEnabled = false;
                this.videoinputPbSucc.Visibility = Visibility.Collapsed;
            }
            else if (type == 2)
            {
                audioMultiInputLists = inputList;
                this.audioinputbox.Text = inputList.Length + LanguageApi.FindRes("p025");
                this.audiooutputbox.Text = inputList.Length + LanguageApi.FindRes("p026");
                this.audiodelaybox.Text = "auto";
                this.audiodelaybox.IsEnabled = false;
                this.audiooutputbox.IsEnabled = false;
                this.audiooutput.IsEnabled = false;
                this.audioinputPbSucc.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 取消多文件输入的快速方法
        /// </summary>
        private void MultiInputClear(int type, bool isClearInput = true)
        {
            //type 1 视频 type 2 音频
            if (type == 1)
            {
                videoMultiInputLists = null;
                if (isClearInput)
                {
                    this.videoinputbox.Text = "";
                }
                this.videooutputbox.Text = "";
                this.videooutputbox.IsEnabled = true;
                this.videooutput.IsEnabled = true;
                this.videoinputPbSucc.Visibility = Visibility.Collapsed;
            }
            else if (type == 2)
            {
                audioMultiInputLists = null;
                if (isClearInput)
                {
                    this.audioinputbox.Text = "";
                }
                this.audiooutputbox.Text = "";
                this.audiooutputbox.IsEnabled = true;
                this.audiooutput.IsEnabled = true;
                this.audiodelaybox.Text = "0";
                this.audiodelaybox.IsEnabled = true;
                this.audioinputPbSucc.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 简易压制视频拖入后处理
        /// </summary>
        private void SimpleVideoInputUpdate()
        {
            string inputStr = simplevideoinputbox.Text;

            this.simplevideoinputPbSucc.Visibility = Visibility.Collapsed;
            this.simplevideoinputPb.Visibility = Visibility.Visible;
            this.simpleAddQueueBtn.IsEnabled = false;
            this.simplePreviewBtn.IsEnabled = false;
            new Thread(
                () =>
                {
                    QueueApi.SimpleEncodeFileInputCheck(inputStr, out string videoinputboxText, out string audioinputboxtext);
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            this.simpleAddQueueBtn.IsEnabled = true;
                            this.simplePreviewBtn.IsEnabled = true;
                            simplevideoinputbox.Text = videoinputboxText;
                            simpleaudioinputbox.Text = audioinputboxtext;
                            this.simplevideoinputPb.Visibility = Visibility.Collapsed;
                            if (videoinputboxText != "")
                            {
                                this.simplevideoinputPbSucc.Visibility = Visibility.Visible;
                                UpdateEncoderSuffix("simpleencode", videoinputboxText, @"_vsgui." + simplemuxsuffixbox.Text.ToLower());
                            }
                        });
                    }
                    catch (Exception)
                    {
                        //执行时可能遇到问题
                    }
                }
            ).Start();



            //QueueApi.SimpleEncodeFileInputCheck(simplevideoinputbox.Text, out string videoinputboxText, out string audioinputboxtext);
            //simplevideoinputbox.Text = videoinputboxText;
            //simpleaudioinputbox.Text = audioinputboxtext;
            //if (videoinputboxText != "")
            //{
            //    UpdateEncoderSuffix("simpleencode", videoinputboxText, @"_vsgui." + simplemuxsuffixbox.Text.ToLower());
            //}
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
                toTextBox.Text = CallSaveFileDialog(filename: Path.GetFileNameWithoutExtension(toTextBox.Text), ext: EncoderApi.GetEncoderSuffix("audio", audioencoderbox.SelectedIndex));
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
            //增加一个编码器路径检测

            if (((ComboBox)sender).Name == "videoencoderbox")
            {
                if (videoinputbox.Text != "")
                {
                    UpdateEncoderSuffix("video", videoinputbox.Text, EncoderApi.GetEncoderSuffix("video", videoencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("videoencoderboxSelectedIndex", videoencoderbox.SelectedIndex.ToString());
            }
            else if (((ComboBox)sender).Name == "audioencoderbox")
            {
                if (audioinputbox.Text != "")
                {
                    UpdateEncoderSuffix("audio", audioinputbox.Text, EncoderApi.GetEncoderSuffix("audio", audioencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("audioencoderboxSelectedIndex", audioencoderbox.SelectedIndex.ToString());
            }
            else if (((ComboBox)sender).Name == "simplevideoencoderbox")
            {
                if (simplevideoinputbox.Text != "")
                {
                    UpdateEncoderSuffix("video", simplevideoinputbox.Text, EncoderApi.GetEncoderSuffix("video", simplevideoencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("simplevideoencoderboxSelectedIndex", simplevideoencoderbox.SelectedIndex.ToString());
            }
            else if (((ComboBox)sender).Name == "simpleaudioencoderbox")
            {
                if (simpleaudioinputbox.Text != "")
                {
                    UpdateEncoderSuffix("audio", simpleaudioinputbox.Text, EncoderApi.GetEncoderSuffix("audio", simpleaudioencoderbox.SelectedIndex));
                }
                IniApi.IniWriteValue("simpleaudioencoderboxSelectedIndex", simpleaudioencoderbox.SelectedIndex.ToString());
            }
        }

        private string UpdateEncoderSuffix(string type, string inputstr, string suffixstr, bool returnMode = false)
        {
            string outputpath = Path.GetDirectoryName(inputstr) + @"\" + Path.GetFileNameWithoutExtension(inputstr) + suffixstr;
            if (File.Exists(outputpath))
            {
                outputpath = Path.GetDirectoryName(inputstr) + @"\" + Path.GetFileNameWithoutExtension(inputstr) + @"-" + DateTime.Now.ToString("yyMMddHHmmss") + suffixstr;
            }
            if (returnMode == false)
            {
                if (type == "video")
                {
                    videooutputbox.Text = outputpath;
                }
                else if (type == "audio")
                {
                    audiooutputbox.Text = outputpath;
                }
                else if (type == "simpleencode")
                {
                    simplevideooutputbox.Text = outputpath;
                }
            }
            return outputpath;
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
            if (videoMultiInputLists != null)
            {
                //多任务
                foreach (var item in videoMultiInputLists)
                {
                    string outputFileName = UpdateEncoderSuffix("video", item, EncoderApi.GetEncoderSuffix("video", videoencoderbox.SelectedIndex), returnMode: true);
                    QueueApi.AddQueueList("video", videoencoderbox.SelectedIndex, new string[] { item }, outputFileName);
                }
            }
            else
            {
                //单任务
                QueueApi.AddQueueList("video", videoencoderbox.SelectedIndex, new string[] { videoinputbox.Text }, videooutputbox.Text);
            }
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
                if (!(audioMultiInputLists != null && audiodelaybox.Text == "auto"))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("delayFormatError"), LanguageApi.FindRes("error"));
                    return;
                }
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
            //音频参数里面东西多，多输入的情况下，需要处理。考虑直接禁用cut。
            if (audioMultiInputLists != null)
            {
                //多任务
                foreach (var item in audioMultiInputLists)
                {
                    string cuttext = "";
                    if (cutischecked.IsChecked == true) cuttext = cuttextbox.Text;
                    string outputFileName = UpdateEncoderSuffix("audio", item, EncoderApi.GetEncoderSuffix("audio", audioencoderbox.SelectedIndex), returnMode: true);
                    QueueApi.AddQueueList("audio", audioencoderbox.SelectedIndex, new string[] { item }, outputFileName, deletefile: item + ".lwi", audiocuttext: cuttext, audiofpstext: fpstextbox.Text, audiodelaytext: "auto");
                }
            }
            else
            {
                //单任务
                string cuttext = "";
                if (cutischecked.IsChecked == true) cuttext = cuttextbox.Text;
                QueueApi.AddQueueList("audio", audioencoderbox.SelectedIndex, new string[] { audioinputbox.Text }, audiooutputbox.Text, deletefile: audioinputbox.Text + ".lwi", audiocuttext: cuttext, audiofpstext: fpstextbox.Text, audiodelaytext: audiodelaybox.Text);
            }

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
            //视频检测
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
            //音频检测
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
            //章节检测
            if (capinputbox.Text != "")
            {
                if (!File.Exists(capinputbox.Text))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFileDoesNotExist"), LanguageApi.FindRes("error"));
                    return;
                }
                if (!ChapterApi.ChapterFormatCheck(capinputbox.Text))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFormatErrorTipsDesc"), LanguageApi.FindRes("error"));
                    return;
                }
            }
            //多文件判断
            if (videoMultiInputLists != null || audioMultiInputLists != null)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p027"), LanguageApi.FindRes("error"));
                return;
            }
            //封装格式选择
            //生成groud名
            string groupname = CommonApi.GetNewSeed();
            QueueApi.AddQueueList("video", videoencoderbox.SelectedIndex, new string[] { videoinputbox.Text }, videooutputbox.Text, group: groupname);
            string cuttext = "";
            if (cutischecked.IsChecked == true) cuttext = cuttextbox.Text;
            QueueApi.AddQueueList("audio", audioencoderbox.SelectedIndex, new string[] { audioinputbox.Text }, audiooutputbox.Text, deletefile: audioinputbox.Text + ".lwi", audiocuttext: cuttext, audiofpstext: fpstextbox.Text, audiodelaytext: audiodelaybox.Text, group: groupname);
            //再添加一个混流任务
            QueueApi.AddQueueList("mux", 0, new string[] { videooutputbox.Text, audiooutputbox.Text }, Path.GetDirectoryName(videooutputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(videooutputbox.Text) + @"_mux." + muxsuffixbox.Text.ToLower(), chapinput: capinputbox.Text, deletefile: videooutputbox.Text + "|" + audiooutputbox.Text, group: groupname);
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
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFormatErrorTipsDesc"), LanguageApi.FindRes("error"));
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
                string[] asspathList = simpleasspathinputbox.Text.Split("|");
                foreach (var item in asspathList)
                {
                    if (item != "")
                    {
                        if (!File.Exists(item))
                        {
                            MessageBoxApi.Show(LanguageApi.FindRes("subtitleFileError"), LanguageApi.FindRes("error"));
                            return;
                        }
                    }
                }
            }
            string tfmEnable = "false";
            if (tfmCheckBox.IsChecked == true)
            {
                tfmEnable = "true";
            }
            string xyvsfilterEnable = "false";
            if (vsfiltermodCheckBox.IsChecked == true)
            {
                xyvsfilterEnable = "true";
            }

            //生成groud名
            string groupname = CommonApi.GetNewSeed();
            string tempvideopath = Path.GetDirectoryName(simplevideooutputbox.Text) + @"\" + groupname + "_v" + EncoderApi.GetEncoderSuffix("video", simplevideoencoderbox.SelectedIndex);
            string tempaudiopath = Path.GetDirectoryName(simplevideooutputbox.Text) + @"\" + groupname + "_a" + EncoderApi.GetEncoderSuffix("audio", simpleaudioencoderbox.SelectedIndex);
            QueueApi.AddQueueList("video", simplevideoencoderbox.SelectedIndex, new string[] { simplevideoinputbox.Text }, tempvideopath, resolution: simpleresolutionbox.Text.ToUpper(), subtitle: simpleasspathinputbox.Text, group: groupname, tfmenable: tfmEnable, xyvsfilterenable: xyvsfilterEnable);
            QueueApi.AddQueueList("audio", simpleaudioencoderbox.SelectedIndex, new string[] { simpleaudioinputbox.Text }, tempaudiopath, deletefile: simpleaudioinputbox.Text + ".lwi", group: groupname);
            //再添加一个混流任务
            QueueApi.AddQueueList("mux", 0, new string[] { tempvideopath, tempaudiopath }, Path.GetDirectoryName(simplevideooutputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(simplevideooutputbox.Text) + @"_mux." + simplemuxsuffixbox.Text.ToLower(), chapinput: simplecapinputbox.Text, deletefile: tempvideopath + "|" + tempaudiopath, group: groupname);
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
            Dispatcher.Invoke(() =>
            {
                this.StartQueueAll.IsEnabled = false;
            });

            new Thread(
                () =>
                {
                    QueueApi.SetQueueListitem(queueid, "status", "running");
                    QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("preparing"));
                    QueueApi.SetQueueListitem(queueid, "starttime", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString());
                    UpdateQueueList(notBtn: true);

                    //特殊格式文件特殊处理
                    QueueApi.SpecialFormatPreProcess(queueid);
                    //判断是否需要生成文件
                    bool scriptSucc = QueueApi.MakeScriptFile(queueid);
                    if (!scriptSucc)
                    {
                        QueueApi.SetQueueListitem(queueid, "status", "error");
                        QueueApi.SetQueueListitem(queueid, "statustext", LanguageApi.FindRes("error") + ": " + LanguageApi.FindRes("p048"));
                        UpdateQueueList(notBtn: true);
                        QueueApi.SaveQueueList();
                        return;
                    }
                    //写入totalframes
                    QueueApi.UpdateTotalframes(queueid);

                    //处理环境path
                    string clipath = QueueApi.GetQueueListitem(queueid, "clipath");
                    if (QueueApi.GetQueueListitem(queueid, "type") == "video")
                    {
                        //使用实时的配置
                        clipath = envpath;
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
                this.QueueSelectItemDesc.Text = LanguageApi.FindRes("queue") + QueueApi.GetQueueListitem(selectQueueid, "queueid") + ": ";
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
                this.QueueSelectItemDesc.Text = "";
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
            if (smuxsubinputbox.Text != "")
            {
                if (!File.Exists(smuxsubinputbox.Text))
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFileDoesNotExist"), LanguageApi.FindRes("error"));
                    return;
                }
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
                    MessageBoxApi.Show(LanguageApi.FindRes("chapterFormatErrorTipsDesc"), LanguageApi.FindRes("error"));
                    return;
                }
            }
            smuxStartButton.IsEnabled = false;
            MuxApi.StartSMux(new string[] { smuxvideoinputbox.Text, smuxaudioinputbox.Text }, smuxsubinputbox.Text, smuxchapterinputbox.Text, smuxsuffixbox.Text.ToLower(), WhenDataReceived, WhenExited);
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
            //判断是否不再提示编辑器安装提示
            if (IniApi.IniReadValue("InstallVseditPrompted") != "true")
            {
                //判断是否未安装
                if (CommonApi.CheckBuildinVSEditorInstall() < 2)
                {
                    //判断是否内置环境，非内置环境不提示
                    if (this.buildinEnvRadio.IsChecked == true)
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
            }

            string filepath = "";
            if (videoinputbox.Text != "" && Path.GetExtension(videoinputbox.Text).ToLower() == ".vpy")
            {
                filepath = videoinputbox.Text;
            }
            if (envpath != null && EnvApi.checkEnvEditorNow() == 1)
            {
                Process.Start(envpath + @"\vsedit.exe", filepath);
            }
            else
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p045"), LanguageApi.FindRes("tips"));
            }

        }

        /// <summary>
        /// 打开编辑器预览按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenEditorPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoinputbox.Text != "" && Path.GetExtension(videoinputbox.Text).ToLower() == ".vpy")
            {
                VideoApi.OpenPreviewWindows(videoinputbox.Text);
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
            UpdateApi.UpdateCheck(proxyUrl.Text, UpdateProgressCall, UpdateFinishCall);
        }

        private void UpdateProgressCall(string message)
        {
            Dispatcher.Invoke(() =>
            {
                updateinfotext.Text = message;
                ReScanButton.IsEnabled = false;
                ReScanButton.Content = message;
                if (message == LanguageApi.FindRes("p033"))
                {
                    BrushConverter brushConverter = new BrushConverter();
                    updateBorder.Background = (Brush)brushConverter.ConvertFromString("#FF5050");
                }
            });
        }

        private void UpdateFinishCall()
        {
            Dispatcher.Invoke(() =>
            {
                ReScanButton.IsEnabled = true;
                ReScanButton.Content = LanguageApi.FindRes("rescan");
                StartQueueItem.IsEnabled = true;
                StartQueueAll.IsEnabled = true;
                demuxStartButton.IsEnabled = true;
                smuxStartButton.IsEnabled = true;
            });
        }

        private void InstallVseditorButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.buildinEnvRadio.IsChecked == true)
            {
                installVsEditorDo();
            }
            else
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p050"), LanguageApi.FindRes("tips"));
            }
        }

        private void installVsEditorDo()
        {
            if (File.Exists(Path.Combine(envpath, @"vsedit.exe")))
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
            else
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p045"), LanguageApi.FindRes("tips"));
            }
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
            if (CommonApi.CheckBuildinVSEditorInstall() == 2)
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

        private void UpdateEnvVersion()
        {
            //还原自定义环境设置
            if (IniApi.IniReadValue("CustomEnvPath") != "")
            {
                if (Directory.Exists(IniApi.IniReadValue("CustomEnvPath")) && File.Exists(IniApi.IniReadValue("CustomEnvPath") + @"\python.exe") && File.Exists(IniApi.IniReadValue("CustomEnvPath") + @"\VSPipe.exe"))
                {
                    this.CustomEnvPathBox.Text = IniApi.IniReadValue("CustomEnvPath");
                }
            }
            string customEnvPath = "";
            if (this.CustomEnvPathBox.Text != "")
            {
                this.CustomEnvPathboxTips.Visibility = Visibility.Collapsed;
                customEnvPath = this.CustomEnvPathBox.Text;
            }

            new Thread(
                () =>
                {
                    //检查内置环境
                    CheckEnvProcess(binpath + @"\vs\", buildinEnvRadio, buildinpyvertext, buildinvsvertext);

                    //检查系统环境
                    CheckEnvProcess("", systemEnvRadio, systempyvertext, systemvsvertext);

                    //检查自定义环境
                    CheckEnvProcess(customEnvPath, customEnvRadio, custompyvertext, customvsvertext);

                    if (CommonApi.CheckBuildinVSEditorInstall() == 2 && IniApi.IniReadValue("EnvironmentType") != "0" && IniApi.IniReadValue("EnvironmentType") != "")
                    {
                        MessageBoxApi.Show(LanguageApi.FindRes("useSystemEnvironmentWarningVsEditor"), LanguageApi.FindRes("tips"));
                    }
                    Dispatcher.Invoke(() =>
                    {
                        //尝试还原环境设置
                        if (IniApi.IniReadValue("EnvironmentType") == "" || IniApi.IniReadValue("EnvironmentType") == "0")
                        {
                            //未设置过或使用内置
                            UseBuildEnv();
                        }
                        else
                        {
                            if (IniApi.IniReadValue("EnvironmentType") == "1") //1是系统
                            {
                                if (this.systemEnvRadio.IsEnabled == true)
                                {
                                    this.systemEnvRadio.IsChecked = true;
                                    envpath = "";
                                }
                                else
                                {
                                    UseBuildEnv();
                                }
                            }
                            else if (IniApi.IniReadValue("EnvironmentType") == "2") //2是自定义
                            {
                                if (this.customEnvRadio.IsEnabled == true)
                                {
                                    this.customEnvRadio.IsChecked = true;
                                    envpath = this.CustomEnvPathBox.Text;
                                }
                                else
                                {
                                    UseBuildEnv();
                                }
                            }
                        }
                    });
                }).Start();

            void CheckEnvProcess(string path, RadioButton radioButton, TextBlock pyVerTextBlock, TextBlock vsVerTextBlock)
            {

                string[] versionList = EnvApi.getEnvVersion(path);
                bool isEnvironmentReady = false;
                if (versionList[0] != null && versionList[1] != null)
                {
                    isEnvironmentReady = true;
                }
                Dispatcher.Invoke(() =>
                {
                    if (isEnvironmentReady == false)
                    {
                        radioButton.IsEnabled = false;
                        radioButton.IsChecked = false;
                    }
                    else
                    {
                        radioButton.IsEnabled = true;
                    }
                    pyVerTextBlock.Text = versionList[0];
                    if (versionList[0] == null) pyVerTextBlock.Text = LanguageApi.FindRes("notInstalled");
                    vsVerTextBlock.Text = versionList[1];
                    if (versionList[1] == null) vsVerTextBlock.Text = LanguageApi.FindRes("notInstalled");
                });

            }

            void UseBuildEnv()
            {
                if (this.buildinEnvRadio.IsEnabled == true)
                {
                    this.buildinEnvRadio.IsChecked = true;
                    envpath = binpath + @"\vs\";
                }
                else
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("p044"), LanguageApi.FindRes("tips"));
                }
            }
        }

        private void StartVsrepoButton_Click(object sender, RoutedEventArgs e)
        {
            //增加环境处理
            if (File.Exists(Path.Combine(envpath, @"VSRepoGUI.exe")))
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = Path.Combine(envpath, @"VSRepoGUI.exe");
                startInfo.WorkingDirectory = envpath;
                Process.Start(startInfo);
            }
            else
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p049"), LanguageApi.FindRes("tips"));
            }
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
            if (this.systemEnvRadio.IsChecked == true)//系统环境打开user
            {
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VapourSynth\plugins64"))
                {
                    System.Diagnostics.Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VapourSynth\plugins64");
                }
                else if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VapourSynth\plugins32"))//补一个找32位
                {
                    System.Diagnostics.Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VapourSynth\plugins32");
                }
                else
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("p046"), LanguageApi.FindRes("tips"));
                }
            }
            else
            {
                if (Directory.Exists(envpath + @"vs-plugins"))//R66起新路径
                {
                    System.Diagnostics.Process.Start("explorer.exe", envpath + @"vs-plugins");
                }
                else if (Directory.Exists(envpath + @"vapoursynth64\plugins"))//R65前老路径
                {
                    System.Diagnostics.Process.Start("explorer.exe", envpath + @"vapoursynth64\plugins");
                }
                else if (Directory.Exists(envpath + @"vapoursynth32\plugins"))//R65前老路径,补一个找32位
                {
                    System.Diagnostics.Process.Start("explorer.exe", envpath + @"vapoursynth32\plugins");
                }
                else
                {
                    MessageBoxApi.Show(LanguageApi.FindRes("p046"), LanguageApi.FindRes("tips"));
                }
            }
        }
        private void OpenScriptsButton_Click(object sender, RoutedEventArgs e)
        {
            //内建环境，找固定目录
            if (this.buildinEnvRadio.IsChecked == true)
            {
                if (Directory.Exists(envpath + @"vs-scripts"))//R66起新路径
                {
                    System.Diagnostics.Process.Start("explorer.exe", envpath + @"vs-scripts");
                }
                else if (Directory.Exists(envpath + @"python\libs"))//R65前老路径
                {
                    System.Diagnostics.Process.Start("explorer.exe", envpath + @"python\libs");
                }
            }

            //系统环境，弹提示
            if (this.systemEnvRadio.IsChecked == true)
            {
                MessageBoxApi.Show(LanguageApi.FindRes("p047"), LanguageApi.FindRes("tips"));
            }

            //自定义环境，回落根目录
            if (this.customEnvRadio.IsChecked == true)
            {
                if (Directory.Exists(envpath + @"vs-scripts"))//R66起新官方默认路径
                {
                    System.Diagnostics.Process.Start("explorer.exe", envpath + @"vs-scripts");
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", envpath);
                }
            }
        }


        private void QueueListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.QueueListView.SelectedIndex = -1;
        }

        private void videoinputbox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputStr = videoinputbox.Text;
            MultiInputClear(1, isClearInput: false);
            if (File.Exists(inputStr) && Path.GetExtension(inputStr) == ".vpy")
            {
                VideoInputUpdate();
            }
            else
            {
                this.videoinputPbSucc.Visibility = Visibility.Collapsed;
            }
        }

        private void simplevideoinputbox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputStr = simplevideoinputbox.Text;
            //MultiInputClear(1, isClearInput: false);
            if (File.Exists(inputStr))
            {
                SimpleVideoInputUpdate();
            }
            else
            {
                this.videoinputPbSucc.Visibility = Visibility.Collapsed;
            }
        }

        private void audioinputbox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputStr = audioinputbox.Text;
            MultiInputClear(2, isClearInput: false);
            if (File.Exists(inputStr))
            {
                AudioInputUpdate();
            }
            else
            {
                this.audioinputPbSucc.Visibility = Visibility.Collapsed;
            }
        }

        private void proxyUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            IniApi.IniWriteValue("proxyurl", this.proxyUrl.Text);
        }

        private void vsfiltermodCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.vsfiltermodCheckBox.IsChecked == true)
            {
                IniApi.IniWriteValue("vsfiltermodEnable", "true");
            }
            else
            {
                IniApi.IniWriteValue("vsfiltermodEnable", "false");
            }
        }
        private string GetSimplePreviewVpyScript()
        {
            if (simplevideoinputbox.Text == "")
            {
                return "";
            }
            bool tfmEnable = false;
            if (tfmCheckBox.IsChecked == true)
            {
                tfmEnable = true;
            }
            bool vsfiltermodEnable = false;
            if (vsfiltermodCheckBox.IsChecked == true)
            {
                vsfiltermodEnable = true;
            }
            string script = VideoApi.MakeVideoScript(simplevideoinputbox.Text, simpleresolutionbox.Text.ToUpper(), simpleasspathinputbox.Text, tfmEnable: tfmEnable, vsfiltermodEnable: vsfiltermodEnable);
            return script;
        }

        private void SimpleOpenEditorButton_Click(object sender, RoutedEventArgs e)
        {
            string script = GetSimplePreviewVpyScript();
            if (script == null)
            {
                MessageBoxApi.Show("DEBUG ERROR: 视频信息读取错误，麻烦提供视频文件协助调试", LanguageApi.FindRes("error"));
                script = "";
            }
            VideoApi.PreviewTempVpy(script);
        }

        private void ExportVpyToDesktop_Click(object sender, RoutedEventArgs e)
        {
            string script = GetSimplePreviewVpyScript();
            if (script == null)
            {
                MessageBoxApi.Show("DEBUG ERROR: 视频信息读取错误，麻烦提供视频文件协助调试", LanguageApi.FindRes("error"));
                script = "";
            }
            string outputPath = Path.GetDirectoryName(simplevideoinputbox.Text) + @"\" + Path.GetFileNameWithoutExtension(simplevideoinputbox.Text) + ".vpy";
            try
            {
                File.WriteAllText(outputPath, script);
            }
            catch (Exception)
            {

            }
        }

        private void envRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).Name.ToString() == "buildinEnvRadio")
            {
                IniApi.IniWriteValue("EnvironmentType", "0");
                envpath = binpath + @"\vs\";
            }
            else if (((RadioButton)sender).Name.ToString() == "systemEnvRadio")
            {
                IniApi.IniWriteValue("EnvironmentType", "1");
                envpath = "";
            }
            else if (((RadioButton)sender).Name.ToString() == "customEnvRadio")
            {
                IniApi.IniWriteValue("EnvironmentType", "2");
                envpath = this.CustomEnvPathBox.Text;
            }

        }


        /// <summary>
        /// 切换混流格式设置时变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void smuxsuffixbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.smuxsuffixbox.SelectedIndex == 0)
            {
                this.subinputbox.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.subinputbox.Visibility = Visibility.Visible;
            }
        }

        private void inputPbSucc_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.Shapes.Path)sender).Visibility = Visibility.Collapsed;
        }
    }
}
