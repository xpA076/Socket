using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Storage
{
    internal class StoragePathMapper
    {
        private static readonly Lazy<StoragePathMapper> _instance = new Lazy<StoragePathMapper>(() => new StoragePathMapper());

        public static StoragePathMapper Instance { get { return _instance.Value; } }

        private StoragePathMapper()
        {
            ;
        }

        public string ConfigPath
        {
            get
            {
                return "";
            }
        }
    }
}
