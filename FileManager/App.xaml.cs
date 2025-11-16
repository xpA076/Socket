using FileManager.Windows.TrayIcon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace FileManager
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private TrayIcon? _trayIcon;


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                //int a = 1;
            }
            else
            {
                //int a = 2;
            }
            return;

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _trayIcon = new TrayIcon();
            _trayIcon.Initialize();
            //Current.MainWindow.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            ((Window)sender).Hide();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnExit(e);
        }


        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var requestedAssemblyName = new AssemblyName(args.Name);
            string resourceName = "FileManager." + requestedAssemblyName.Name + ".dll";
            System.Windows.Forms.MessageBox.Show(resourceName);
            //string resourceName = "SocketLib";
            try
            {
                using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    return Assembly.Load(buffer);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(string.Format(@"{0}\{1}", ex.Message, ex.StackTrace));
                throw;
            }
        }
    }
}
