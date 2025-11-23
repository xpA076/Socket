using FileManager.Models.EncryptLib;
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

namespace FileManager.Services.Certificate
{
    public sealed class CertificateService : IDisposable
    {
        public enum Side
        {
            Client,
            Server
        }

        private readonly KeyStorage keyStorage = Program.Provider.GetRequiredService<KeyStorage>();

        private EcdhManager client;

        private EcdhManager server;


        public CertificateService()
        {
            client = new EcdhManager(keyStorage.ClientPrivateKeyBytesPkcs8);
            server = new EcdhManager(keyStorage.ServerPrivateKeyBytesPkcs8);
        }

        public KeyExchangeMessage CreateKeyExchangeMessage(Side side)
        {
            if (side == Side.Client)
            {
                return EcdhKeyExchangeProtocol.CreateKeyExchangeMessage(client);
            }
            if (side == Side.Server)
            {
                return EcdhKeyExchangeProtocol.CreateKeyExchangeMessage(server);
            }
            throw new ArgumentException(side.ToString());
        }

        public static bool VerifyKeyExchangeMessage(KeyExchangeMessage message)
        {
            return EcdhKeyExchangeProtocol.VerifyKeyExchangeMessage(message);
        }

        public byte[] DeriveAes256Key(KeyExchangeMessage message, Side side, byte[] salt)
        {
            if (side == Side.Client) 
            {
                var sharedSecret = client.DeriveSharedSecret(message.EphemeralPublicKey);
                return EcdhManager.DeriveAes256Key(sharedSecret, salt);
            }
            if (side == Side.Server)
            {
                var sharedSecret = server.DeriveSharedSecret(message.EphemeralPublicKey);
                return EcdhManager.DeriveAes256Key(sharedSecret, salt);
            }
            throw new ArgumentException(side.ToString());
        }



        public void Dispose()
        {
            client?.Dispose();
            server?.Dispose();
        }

        public void t1()
        {
            var a = Convert.ToBase64String(client.IdentityPrivateKeyBytesPkcs8);
            var b = Convert.ToBase64String(server.IdentityPrivateKeyBytesPkcs8);
            int i = 1;
        }

        public bool IsTrustedEndPoint(byte[] identityPrivetaBytes, Side side)
        {
            return true;
        }

    }
}
