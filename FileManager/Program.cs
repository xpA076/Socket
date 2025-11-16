using FileManager.Models.Config;
using FileManager.Models.Log;
using FileManager.Models.SocketLib.Services;
using FileManager.Utils;
using FileManager.Utils.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public static class Program
    {
        public static readonly ServiceCollection Services = new ServiceCollection();

        public static ServiceProvider Provider {  get; private set; } = Services.BuildServiceProvider();


        [STAThread]
        public static void Main(string[] args)
        {
            TestEncrypt te = new TestEncrypt();
            te.Test1();

            Services.AddSingleton<KeyStorage>();
            Services.AddSingleton<CertificateService>();
            Services.AddSingleton<StoragePathMapper>();
            Services.AddSingleton<ClientConfigStorage>();
            Services.AddSingleton<ConfigService>();
            Services.AddSingleton<LogService>();

            /// Build provider
            Program.Provider = Services.BuildServiceProvider();

            //var a = Provider.GetService<KeyStorage>();
            FileManager.App app = new FileManager.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
