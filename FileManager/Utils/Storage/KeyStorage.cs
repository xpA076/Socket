using FileManager.Models.Serializable.Crypto;
using FileManager.Models.SocketLib;
using FileManager.Models.SocketLib.Services;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FileManager.Utils.Storage
{
    public sealed class KeyStorage
    {
        private static readonly KeyStorage _instance = new KeyStorage();

        public static KeyStorage Instance { get { return _instance; } }

        private readonly CertificateService CertificateService = CertificateService.Instance;

        private readonly StoragePathMapper PathMapper = StoragePathMapper.Instance;

        private SocketPrivateKey _clientPrivateKey = new SocketPrivateKey();

        private SocketPrivateKey _serverPrivateKey = new SocketPrivateKey();

        private List<SocketCertificate> _trustedCertificasteList = new List<SocketCertificate>();

        private KeyStorage() 
        {
            /// Client certificate
            if (File.Exists(PathMapper.ClientPrivateKeyPath))
            {
                _clientPrivateKey = this.LoadKey(PathMapper.ClientPrivateKeyPath);
            }
            else
            {
                _clientPrivateKey = CertificateService.GenerateTemporaryKey();
                this.SaveKey(_clientPrivateKey, PathMapper.ClientPrivateKeyPath);
                this.SaveKey(_clientPrivateKey.Certificate, PathMapper.ClientCertificatePath);
            }
            /// Server certificate
            if (File.Exists(PathMapper.ServerPrivateKeyPath))
            {
                _serverPrivateKey = this.LoadKey(PathMapper.ServerPrivateKeyPath);
            }
            else
            {
                _serverPrivateKey = CertificateService.GenerateTemporaryKey();
                this.SaveKey(_serverPrivateKey, PathMapper.ServerPrivateKeyPath);
                this.SaveKey(_serverPrivateKey.Certificate, PathMapper.ServerCertificatePath);
            }
            /// Trusted certificate
            if (File.Exists(PathMapper.TrustedCertificatePath))
            {
                _trustedCertificasteList = LoadTrustedCertificate(PathMapper.TrustedCertificatePath);
            }
            else
            {
                _trustedCertificasteList = new List<SocketCertificate>();
                SaveTrustedCertificate(PathMapper.TrustedCertificatePath);
            }
        }

        public SocketPrivateKey ClientPrivateKey
        {
            get
            {
                return _clientPrivateKey;
            }
            set 
            {
                _clientPrivateKey = value;
                this.SaveKey(_clientPrivateKey, PathMapper.ClientPrivateKeyPath);
                this.SaveKey(_clientPrivateKey.Certificate, PathMapper.ClientCertificatePath);
            }
        }

        public SocketCertificate ClientCertificate
        {
            get
            {
                return _clientPrivateKey.Certificate;
            }
        }

        public SocketPrivateKey ServerPrivateKey
        {
            get
            {
                return _serverPrivateKey;
            }
            set
            {
                _serverPrivateKey = value;
                this.SaveKey(_serverPrivateKey, PathMapper.ServerPrivateKeyPath);
                this.SaveKey(_serverPrivateKey.Certificate, PathMapper.ServerCertificatePath);
            }
        }

        public SocketCertificate ServerCertificate
        {
            get
            {
                return _serverPrivateKey.Certificate;
            }
        }


        private void SaveKey(IBytesSerializable key, string path)
        {
            File.WriteAllBytes(path, key.ToBytes());
        }

        private SocketPrivateKey LoadKey(string path)
        {
            SocketPrivateKey key = new SocketPrivateKey();
            int idx = 0;
            key.BuildFromBytes(File.ReadAllBytes(path), ref idx);
            return key;
        }

        private void SaveTrustedCertificate(string path)
        {
            BytesBuilder bb = new BytesBuilder();
            bb.AppendList<SocketCertificate>(this._trustedCertificasteList);
            File.WriteAllBytes(path, bb.GetBytes()); ;
        }

        private List<SocketCertificate> LoadTrustedCertificate(string path)
        {
            int idx = 0;
            return BytesParser.GetListSerializable<SocketCertificate>(File.ReadAllBytes(path), ref idx);
        }


    }
}
