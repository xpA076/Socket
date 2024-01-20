using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Storage
{
    internal class StoragePathMapper
    {
        private static readonly Lazy<StoragePathMapper> _instance = new Lazy<StoragePathMapper>(() => new StoragePathMapper());

        public static StoragePathMapper Instance { get { return _instance.Value; } }

        private readonly bool IsDebug = true;

        private StoragePathMapper()
        {
            ;
        }

        private string ProgramDataPath
        {
            get
            {
                string main_dir;
                if (IsDebug)
                {
                    main_dir = Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName;
                    
                }
                else
                {
                    main_dir = Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "FileManager");
                }
                return Path.Combine(main_dir, "storage");
            }
        }


        public string ConfigPath
        {
            get
            {
                return "";
            }
        }

        public string ClientPrivateKeyPath
        {
            get
            {
                return Path.Combine(this.ProgramDataPath, "client_prv_key.fms");
            }
        }

        public string ClientCertificatePath
        {
            get
            {
                return Path.Combine(this.ProgramDataPath, "client_cert.fms");
            }
        }

        public string ServerPrivateKeyPath
        {
            get
            {
                return Path.Combine(this.ProgramDataPath, "server_prv_key.fms");
            }
        }

        public string ServerCertificatePath
        {
            get
            {
                return Path.Combine(this.ProgramDataPath, "server_cert.fms");
            }
        }

        public string TrustedCertificatePath
        {
            get
            {
                return Path.Combine(this.ProgramDataPath, "trusted_cert.fms");
            }
        }
    }
}
