using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MessageBoxButton = VSGUI.MessageWindow.MessageBoxButton;

namespace VSGUI.API
{
    internal class MessageBoxApi
    {
        public static MessageWindow.MessageResult Show(string label, string title = null, MessageBoxButton buttontype = MessageBoxButton.OK)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                MessageWindow mw = new MessageWindow();
                mw.desctext.Text = label;
                if (title == null)
                {
                    title = LanguageApi.FindRes("tips");
                }
                mw.messagewindow.Title = title;
                switch (buttontype)
                {
                    case MessageBoxButton.OK:
                        mw.okbutton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.OKCancel:
                        mw.okbutton.Visibility = Visibility.Visible;
                        mw.Cancelbutton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.Yes:
                        mw.yesbutton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.YesNo:
                        mw.yesbutton.Visibility = Visibility.Visible;
                        mw.nobutton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.YesNoCancel:
                        mw.yesbutton.Visibility = Visibility.Visible;
                        mw.nobutton.Visibility = Visibility.Visible;
                        mw.Cancelbutton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.YesNoNomore:
                        mw.yesbutton.Visibility = Visibility.Visible;
                        mw.nobutton.Visibility = Visibility.Visible;
                        mw.nomorepromptsbutton.Visibility = Visibility.Visible;
                        break;
                    default:
                        break;
                }
                mw.Owner = (MainWindow)Application.Current.MainWindow;
                mw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                mw.ShowDialog();
                return mw.result;
            });
        }

    }
}
