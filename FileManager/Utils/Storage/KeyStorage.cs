using FileManager.Models.EncryptLib;
using FileManager.Models.Serializable.Crypto;
using FileManager.Models.SocketLib;
using FileManager.Services.Certificate;
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
        private readonly StoragePathMapper PathMapper = Program.Provider.GetRequiredService<StoragePathMapper>();


        private string ClientPrivateKeyPath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "ClientPrivateKey.pem");
            }
        }

        private string ServerPrivateKeyPath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "ServerPrivateKey.pem");
            }
        }

        private string TrustedClientPath
        {
            get
            {
                return Path.Combine(PathMapper.CertificateDirectory, "trusted_client_cert.fms");
            }
        }

        public byte[] ClientPrivateKeyBytesPkcs8
        {
            get
            {
                return KeyStorage.LoadPrivateKeysPEM(ClientPrivateKeyPath);
            }
        }

        public byte[] ServerPrivateKeyBytesPkcs8
        {
            get
            {
                return KeyStorage.LoadPrivateKeysPEM(ServerPrivateKeyPath);
            }
        }

        public KeyStorage() 
        {
            /// Client private key
            if (!File.Exists(this.ClientPrivateKeyPath))
            {
                byte[] privateKeyBytes = EcdhManager.GeneratePrivateKeyBytesPkcs8();
                KeyStorage.SavePrivateKeysPEM(privateKeyBytes, this.ClientPrivateKeyPath);
            }
            if (!File.Exists(this.ServerPrivateKeyPath))
            {
                byte[] privateKeyBytes = EcdhManager.GeneratePrivateKeyBytesPkcs8();
                KeyStorage.SavePrivateKeysPEM(privateKeyBytes, this.ServerPrivateKeyPath);
            }
            /// todo : load trusted public keys

        }

        private static void SavePrivateKeysPEM(byte[] private_key_bytes, string save_path)
        {
            string privateKeyPem = 
                "-----BEGIN PRIVATE KEY-----\n" +
                Convert.ToBase64String(private_key_bytes, Base64FormattingOptions.InsertLineBreaks) +
                "\n-----END PRIVATE KEY-----";
            File.WriteAllText(save_path, privateKeyPem);
        }


        private static byte[] LoadPrivateKeysPEM(string load_path)
        {
            string pem = File.ReadAllText(load_path);
            string base64 = pem.Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Trim();
            byte[] privateKeyBytes = Convert.FromBase64String(base64);
            return privateKeyBytes;
        }


    }
}
