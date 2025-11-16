using FileManager.Models.Serializable.Crypto;
using FileManager.Models.SocketLib;
using FileManager.Models.SocketLib.Services;
using FileManager.Utils.Bytes;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly StoragePathMapper PathMapper = Program.Provider.GetService<StoragePathMapper>();

        private SocketPrivateKey _clientPrivateKey = new SocketPrivateKey();

        private SocketPrivateKey _serverPrivateKey = new SocketPrivateKey();

        private List<SocketCertificate> _trustedClientCertificateList = new List<SocketCertificate>();

        private List<SocketCertificate> _trustedServerCertificateList = new List<SocketCertificate>();

        private string ClientPrivateKeyPath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "client_prv_key.fms");
            }
        }

        private string ClientCertificatePath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "client_cert.fms");
            }
        }

        private string ServerPrivateKeyPath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "server_prv_key.fms");
            }
        }

        private string ServerCertificatePath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "server_cert.fms");
            }
        }

        private string TrustedClientCertificatePath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "trusted_client_cert.fms");
            }
        }

        private string TrustedServerCertificatePath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "trusted_server_cert.fms");
            }
        }

        public KeyStorage() 
        {
            /// Client certificate
            if (File.Exists(this.ClientPrivateKeyPath))
            {
                _clientPrivateKey = this.LoadKey(this.ClientPrivateKeyPath);
            }
            else
            {
                _clientPrivateKey = CertificateService.GenerateTemporaryKeyPair();
                SaveKey(_clientPrivateKey, this.ClientPrivateKeyPath);
                SaveKey(_clientPrivateKey.Certificate, this.ClientCertificatePath);
            }
            /// Server certificate
            if (File.Exists(this.ServerPrivateKeyPath))
            {
                _serverPrivateKey = this.LoadKey(this.ServerPrivateKeyPath);
            }
            else
            {
                _serverPrivateKey = CertificateService.GenerateTemporaryKeyPair();
                SaveKey(_serverPrivateKey, this.ServerPrivateKeyPath);
                SaveKey(_serverPrivateKey.Certificate, this.ServerCertificatePath);
            }
            /// Trusted client certificate
            if (File.Exists(this.TrustedClientCertificatePath))
            {
                _trustedClientCertificateList = LoadTrustedCertificate(this.TrustedClientCertificatePath);
            }
            else
            {
                _trustedClientCertificateList = new List<SocketCertificate>();
                SaveTrustedCertificate(this._trustedClientCertificateList, this.TrustedClientCertificatePath);
            }
            if (File.Exists(this.TrustedServerCertificatePath))
            {
                _trustedServerCertificateList = LoadTrustedCertificate(this.TrustedServerCertificatePath);
            }
            else
            {
                _trustedServerCertificateList = new List<SocketCertificate>();
                SaveTrustedCertificate(this._trustedServerCertificateList, this.TrustedServerCertificatePath);
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
                SaveKey(_clientPrivateKey, this.ClientPrivateKeyPath);
                SaveKey(_clientPrivateKey.Certificate, this.ClientCertificatePath);
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
                SaveKey(_serverPrivateKey, this.ServerPrivateKeyPath);
                SaveKey(_serverPrivateKey.Certificate, this.ServerCertificatePath);
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
