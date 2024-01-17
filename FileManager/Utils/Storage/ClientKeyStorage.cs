using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Storage
{
    public sealed class ClientKeyStorage
    {
        private static readonly Lazy<ClientKeyStorage> _instance = new Lazy<ClientKeyStorage>(() => new ClientKeyStorage());

        public static ClientKeyStorage Instance { get { return _instance.Value; } }

        private ClientKeyStorage() 
        { 
            
        }

        private readonly StoragePathMapper PathMapper = StoragePathMapper.Instance;


        public
    }
}
