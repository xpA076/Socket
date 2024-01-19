using FileManager.Models.Serializable.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Storage
{
    public sealed class KeyStorage
    {
        private static readonly Lazy<KeyStorage> _instance = new Lazy<KeyStorage>(() => new KeyStorage());

        public static KeyStorage Instance { get { return _instance.Value; } }

        private KeyStorage() 
        { 
            
        }

        private readonly StoragePathMapper PathMapper = StoragePathMapper.Instance;

        private SocketPrivateKey _clientPrivateKey = new SocketPrivateKey();

        public SocketPrivateKey ClientPrivateKey
        {
            get
            {
                return _clientPrivateKey;
            }
        }


    }
}
