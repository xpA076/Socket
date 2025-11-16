using FileManager.Models.Serializable.Crypto;
using FileManager.Utils.Storage;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly KeyStorage KeyStorage = Program.Provider.GetService<KeyStorage>();

        public CertificateService()
        {

        }

        public SocketCertificate ClientCertificate
        {
            get
            {
                return KeyStorage.ClientCertificate;
            }
        }

        public SocketCertificate ServerCertificate
        {
            get
            {
                return KeyStorage.ServerCertificate;
            }
        }


        public bool IsTrustedServerCertificate(SocketCertificate certificate)
        {
            if (!VerifyCertificate(certificate)) return false;


            return true;
        }

        public bool IsTrustedClientCertificate(SocketCertificate certificate)
        {
            if (!VerifyCertificate(certificate)) return false;


            return true;
        }


        public byte[] ClientSign(byte[] bytes)
        {
            return Sign(bytes, KeyStorage.ClientPrivateKey);
        }

        public byte[] ServerSign(byte[] bytes)
        {
            return Sign(bytes, KeyStorage.ServerPrivateKey);
        }

        public bool VerifyCertificate(SocketCertificate certificate)
        {
            return Verify(certificate.InfoToBytes(), certificate.Signature, certificate);
        }

        public static byte[] Sign(byte[] bytes, SocketPrivateKey privateKey)
        {
            using (ECDsaCng dsa = new ECDsaCng(CngKey.Import(privateKey.PrivateKey, CngKeyBlobFormat.EccPrivateBlob)))
            {
                dsa.HashAlgorithm = CngAlgorithm.Sha256;
                return dsa.SignData(bytes);
            }
        }

        public static bool Verify(byte[] bytes, byte[] signature, SocketCertificate certificate)
        {
            using(ECDsaCng dsa = new ECDsaCng(CngKey.Import(certificate.PublicKey, CngKeyBlobFormat.EccPublicBlob))) 
            {
                return dsa.VerifyData(bytes, signature);
            }
        }

        public static SocketPrivateKey GenerateTemporaryKeyPair()
        {
            SocketPrivateKey privateKey = new SocketPrivateKey();
            privateKey.Certificate.StartTime = DateTime.Now;
            privateKey.Certificate.ExpireTime = DateTime.Now.AddDays(1);
            GeneratePrivateKey(ref privateKey);
            return privateKey;
        }

        private static void GeneratePrivateKey(ref SocketPrivateKey privateKey) 
        { 
            using(ECDsaCng dsa =  new ECDsaCng()) 
            {
                dsa.HashAlgorithm = CngAlgorithm.Sha256;
                privateKey.Certificate.PublicKey = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);
                privateKey.PrivateKey = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                /// Sign public key
                privateKey.Certificate.Signature = CertificateService.Sign(privateKey.Certificate.InfoToBytes(), privateKey);
            }
        }
    }
}
