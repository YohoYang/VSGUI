using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VSGUI.API;

namespace VSGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterEvents();
            base.OnStartup(e);
        }

        private void RegisterEvents()
        {
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                args.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                LogApi.WriteCrashLog(args.ExceptionObject.ToString());
                MessageBox.Show(LanguageApi.FindRes("p004"), LanguageApi.FindRes("error"));
                Application.Current.Shutdown();
            };
        }

        //private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        //{
        //    LogApi.WriteCrashLog(e.Exception.Message);
        //    e.Handled = true;
        //}
    }
}
