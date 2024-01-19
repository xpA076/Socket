using FileManager.Models.Serializable.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.Services
{
    internal class CertificateService
    {
        private static readonly Lazy<CertificateService> _instance = new Lazy<CertificateService>(() => new CertificateService());

        public static CertificateService Instance { get { return _instance.Value; } }

        private CertificateService()
        {

        }

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

        public void GenerateKey(ref SocketPrivateKey privateKey) 
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
