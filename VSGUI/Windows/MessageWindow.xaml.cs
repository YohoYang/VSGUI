using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VSGUI
{
    /// <summary>
    /// MessageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MessageWindow : Window
    {
        public enum MessageResult
        {
            Yes,
            No,
            OK,
            Cancel,
            NoMore
        }
        public enum MessageBoxButton
        {
            OK,
            OKCancel,
            Yes,
            YesNo,
            YesNoCancel,
            YesNoNomore
        }
        public MessageResult result;


        public MessageWindow()
        {
            InitializeComponent();
            System.Media.SystemSounds.Exclamation.Play();
            this.Activate();
            result = MessageResult.Cancel;
        }

        private void yesbutton_Click(object sender, RoutedEventArgs e)
        {
            result = MessageResult.Yes;
            this.Close();
        }

        private void nobutton_Click(object sender, RoutedEventArgs e)
        {
            result = MessageResult.No;
            this.Close();
        }
        private void okbutton_Click(object sender, RoutedEventArgs e)
        {
            result = MessageResult.OK;
            this.Close();
        }

        private void Cancelbutton_Click(object sender, RoutedEventArgs e)
        {
            result = MessageResult.Cancel;
            this.Close();
        }

        private void nomorepromptsbutton_Click(object sender, RoutedEventArgs e)
        {
            result = MessageResult.NoMore;
            this.Close();
        }
    }
}
