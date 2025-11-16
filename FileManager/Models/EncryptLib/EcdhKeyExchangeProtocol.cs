using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.EncryptLib
{
    public class EcdhKeyExchangeProtocol
    {
        public class KeyExchangeMessage
        {
            public required byte[] EphemeralPublicKey { get; set; }
            public required byte[] IdentityPublicKey { get; set; }
            public required byte[] Signature { get; set; }
            public required byte[] Timestamp { get; set; }
            public required byte[] Salt { get; set; }
        }

        public class EncryptedMessage
        {
            public required byte[] Data { get; set; }
            public required byte[] AssociatedData { get; set; }
        }

        /// <summary>
        /// 创建密钥交换消息
        /// </summary>
        public static KeyExchangeMessage CreateKeyExchangeMessage(EcdhManager party)
        {
            var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var ephemeralPublicKey = party.GetPublicKey();
            var salt = EcdhManager.GenerateRandomSalt();

            // 创建签名数据
            var dataToSign = CombineData(ephemeralPublicKey, timestamp, salt);
            var signature = party.SignData(dataToSign);

            return new KeyExchangeMessage
            {
                EphemeralPublicKey = ephemeralPublicKey,
                IdentityPublicKey = party.GetIdentityPublicKey(),
                Signature = signature,
                Timestamp = timestamp,
                Salt = salt
            };
        }

        /// <summary>
        /// 验证密钥交换消息
        /// </summary>
        public static bool VerifyKeyExchangeMessage(KeyExchangeMessage message)
        {
            // 验证时间戳
            if (!VerifyTimestamp(message.Timestamp))
                return false;

            // 验证签名
            var dataToVerify = CombineData(message.EphemeralPublicKey, message.Timestamp, message.Salt);
            return EcdhManager.VerifySignature(dataToVerify, message.Signature, message.IdentityPublicKey);
        }

        private static byte[] CombineData(byte[] publicKey, byte[] timestamp, byte[] salt)
        {
            using var ms = new MemoryStream();
            ms.Write(publicKey, 0, publicKey.Length);
            ms.Write(timestamp, 0, timestamp.Length);
            ms.Write(salt, 0, salt.Length);
            return ms.ToArray();
        }

        private static bool VerifyTimestamp(byte[] timestampBytes)
        {
            try
            {
                var timestamp = new DateTime(BitConverter.ToInt64(timestampBytes, 0));
                return (DateTime.UtcNow - timestamp).TotalMinutes <= 5; // 5分钟有效期
            }
            catch
            {
                return false;
            }
        }
    }
}
