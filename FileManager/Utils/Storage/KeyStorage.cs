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
        private readonly StoragePathMapper PathMapper = StoragePathMapper.Instance;

        private SocketPrivateKey _clientPrivateKey = new SocketPrivateKey();

        private SocketPrivateKey _serverPrivateKey = new SocketPrivateKey();

        private List<SocketCertificate> _trustedClientCertificateList = new List<SocketCertificate>();

        private List<SocketCertificate> _trustedServerCertificateList = new List<SocketCertificate>();

        public KeyStorage() 
        {
            /// Client certificate
            if (File.Exists(PathMapper.ClientPrivateKeyPath))
            {
                _clientPrivateKey = this.LoadKey(PathMapper.ClientPrivateKeyPath);
            }
            else
            {
                _clientPrivateKey = CertificateService.GenerateTemporaryKeyPair();
                SaveKey(_clientPrivateKey, PathMapper.ClientPrivateKeyPath);
                SaveKey(_clientPrivateKey.Certificate, PathMapper.ClientCertificatePath);
            }
            /// Server certificate
            if (File.Exists(PathMapper.ServerPrivateKeyPath))
            {
                _serverPrivateKey = this.LoadKey(PathMapper.ServerPrivateKeyPath);
            }
            else
            {
                _serverPrivateKey = CertificateService.GenerateTemporaryKeyPair();
                SaveKey(_serverPrivateKey, PathMapper.ServerPrivateKeyPath);
                SaveKey(_serverPrivateKey.Certificate, PathMapper.ServerCertificatePath);
            }
            /// Trusted client certificate
            if (File.Exists(PathMapper.TrustedClientCertificatePath))
            {
                _trustedClientCertificateList = LoadTrustedCertificate(PathMapper.TrustedClientCertificatePath);
            }
            else
            {
                _trustedClientCertificateList = new List<SocketCertificate>();
                SaveTrustedCertificate(this._trustedClientCertificateList, PathMapper.TrustedClientCertificatePath);
            }
            if (File.Exists(PathMapper.TrustedServerCertificatePath))
            {
                _trustedServerCertificateList = LoadTrustedCertificate(PathMapper.TrustedServerCertificatePath);
            }
            else
            {
                _trustedServerCertificateList = new List<SocketCertificate>();
                SaveTrustedCertificate(this._trustedServerCertificateList, PathMapper.TrustedServerCertificatePath);
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
                SaveKey(_clientPrivateKey, PathMapper.ClientPrivateKeyPath);
                SaveKey(_clientPrivateKey.Certificate, PathMapper.ClientCertificatePath);
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
                SaveKey(_serverPrivateKey, PathMapper.ServerPrivateKeyPath);
                SaveKey(_serverPrivateKey.Certificate, PathMapper.ServerCertificatePath);
            }
        }

        public SocketCertificate ServerCertificate
        {
            get
            {
                return _serverPrivateKey.Certificate;
            }
        }


        private static void SaveKey(IBytesSerializable key, string path)
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

        private static void SaveTrustedCertificate(List<SocketCertificate> ls, string path)
        {
            BytesBuilder bb = new BytesBuilder();
            bb.AppendList<SocketCertificate>(ls);
            File.WriteAllBytes(path, bb.GetBytes()); ;
        }

        private List<SocketCertificate> LoadTrustedCertificate(string path)
        {
            int idx = 0;
            return BytesParser.GetListSerializable<SocketCertificate>(File.ReadAllBytes(path), ref idx);
        }

    }
}
