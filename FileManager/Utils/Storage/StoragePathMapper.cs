using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Storage
{
    internal class StoragePathMapper
    {
        private readonly bool IsDebug = true;

        public StoragePathMapper()
        {
            ;
        }

        private string ProgramDataDirectory
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

        public string LogDirectory
        {
            get
            {
                //return ConfigDir;
                return this.ProgramDataDirectory;
            }
        }

        public string ConfigPath
        {
            get
            {
                return Path.Combine(this.ProgramDataDirectory, "FileManager.config");
            }
        }


        public string ServerConfigPath
        {
            get
            {
                return Path.Combine(this.ProgramDataDirectory, "FileManagerServer.config");
            }
        }

        public string CertificateDirectory
        {
            get
            {
                return this.ProgramDataDirectory;
            }
        }

    }
}
