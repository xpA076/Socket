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
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                int a = 1;
            }
            else
            {
                int a = 2;
            }
            return;
            //CatchException();
            //AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            /*
            AppDomain.CurrentDomain.AssemblyResolve += (_sender, _args) =>
            {
                //return null;
                string projectName = Assembly.GetExecutingAssembly().GetName().Name.ToString();
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(projectName + ".SocketLib.dll"))
                {
                    byte[] b = new byte[stream.Length];
                    stream.Read(b, 0, b.Length);
                    return Assembly.Load(b);
                }
            };
            */
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
