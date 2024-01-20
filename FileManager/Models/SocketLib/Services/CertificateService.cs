using FileManager.Models.Serializable.Crypto;
using FileManager.Utils.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FileManager.Models.SocketLib.Services
{
    internal class CertificateService
    {
        private static readonly CertificateService _instance = new CertificateService();

        public static CertificateService Instance { get { return _instance; } }

        private CertificateService()
        {

        }

        private readonly KeyStorage KeyStorage = KeyStorage.Instance;

        public bool IsTrustedCertificate(SocketCertificate certificate)
        {
            if (!VerifyCertificate(certificate)) return false;


            return true;
        }

        public bool VerifyCertificate(SocketCertificate certificate)
        {
            return Verify(certificate.InfoToBytes(), certificate.Signature, certificate);
        }

        public byte[] Sign(byte[] bytes, SocketPrivateKey privateKey)
        {
            using (ECDsaCng dsa = new ECDsaCng(CngKey.Import(privateKey.PrivateKey, CngKeyBlobFormat.EccPrivateBlob)))
            {
                dsa.HashAlgorithm = CngAlgorithm.Sha256;
                return dsa.SignData(bytes);
            }
        }

        public bool Verify(byte[] bytes, byte[] signature, SocketCertificate certificate)
        {
            using(ECDsaCng dsa = new ECDsaCng(CngKey.Import(certificate.PublicKey, CngKeyBlobFormat.EccPublicBlob))) 
            {
                return dsa.VerifyData(bytes, signature);
            }
        }

        public SocketPrivateKey GenerateTemporaryKey()
        {
            SocketPrivateKey privateKey = new SocketPrivateKey();
            privateKey.Certificate.StartTime = DateTime.Now;
            privateKey.Certificate.ExpireTime = DateTime.Now.AddDays(1);
            GenerateKey(ref privateKey);
            return privateKey;
        }

        private void GenerateKey(ref SocketPrivateKey privateKey) 
        { 
            using(ECDsaCng dsa =  new ECDsaCng()) 
            {
                dsa.HashAlgorithm = CngAlgorithm.Sha256;
                privateKey.Certificate.PublicKey = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);
                privateKey.PrivateKey = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                /// Sign public key
                privateKey.Certificate.Signature = this.Sign(privateKey.Certificate.InfoToBytes(), privateKey);
            }
        }
    }
}
