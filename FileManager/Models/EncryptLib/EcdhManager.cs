using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.EncryptLib
{

    public class EcdhManager : IDisposable
    {
        private readonly ECDiffieHellman _ephemeralKeyPair;
        private readonly ECDsa _identityKey;
        private const int TagSizeInBytes = 16; // 128-bit authentication tag

        public EcdhManager()
        {
            _ephemeralKeyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            _identityKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        }

        /// <summary>
        /// 获取公钥
        /// </summary>
        public byte[] GetPublicKey()
        {
            return _ephemeralKeyPair.ExportSubjectPublicKeyInfo();
        }

        public byte[] GetIdentityPublicKey()
        {
            return _identityKey.ExportSubjectPublicKeyInfo();
        }

        /// <summary>
        /// 执行ECDH密钥协商
        /// </summary>
        public byte[] DeriveSharedSecret(byte[] otherPartyPublicKey)
        {
            using var otherParty = ECDiffieHellman.Create();
            otherParty.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);

            return _ephemeralKeyPair.DeriveKeyFromHash(
                otherParty.PublicKey,
                HashAlgorithmName.SHA256
            );
        }

        /// <summary>
        /// 使用HKDF从共享密钥派生AES256密钥
        /// </summary>
        public static byte[] DeriveAes256Key(byte[] sharedSecret, byte[] salt = null, byte[] info = null)
        {
            if (sharedSecret == null || sharedSecret.Length == 0)
                throw new ArgumentException("共享密钥不能为空", nameof(sharedSecret));

            salt ??= Encoding.UTF8.GetBytes("ECDH-AES256-KEY-DERIVATION");
            info ??= Encoding.UTF8.GetBytes("AES-256-GCM-KEY");

            // HKDF提取步骤
            using var hmac = new HMACSHA256(salt);
            byte[] pseudoRandomKey = hmac.ComputeHash(sharedSecret);

            // HKDF扩展步骤
            return HkdfExpand(pseudoRandomKey, info, 32);
        }

        private static byte[] HkdfExpand(byte[] prk, byte[] info, int outputLength)
        {
            if (prk == null) throw new ArgumentNullException(nameof(prk));
            if (outputLength < 1 || outputLength > 255 * 32)
                throw new ArgumentOutOfRangeException(nameof(outputLength));

            using var hmac = new HMACSHA256(prk);
            var result = new byte[outputLength];
            byte[] previous = Array.Empty<byte>();
            int bytesGenerated = 0;
            byte counter = 1;

            while (bytesGenerated < outputLength)
            {
                hmac.Initialize();

                if (previous.Length > 0)
                    hmac.TransformBlock(previous, 0, previous.Length, null, 0);

                if (info != null && info.Length > 0)
                    hmac.TransformBlock(info, 0, info.Length, null, 0);

                hmac.TransformBlock(new[] { counter }, 0, 1, null, 0);
                hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                previous = hmac.Hash;
                int bytesToCopy = Math.Min(previous.Length, outputLength - bytesGenerated);
                Buffer.BlockCopy(previous, 0, result, bytesGenerated, bytesToCopy);
                bytesGenerated += bytesToCopy;
                counter++;
            }

            return result;
        }

        /// <summary>
        /// 使用AES-GCM加密（符合.NET 8.0 API）
        /// </summary>
        public static byte[] EncryptWithAesGcm(byte[] plaintext, byte[] key, byte[] associatedData = null)
        {
            // GCM推荐使用12字节的nonce
            byte[] nonce = new byte[12];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);

            // 创建认证标签
            byte[] tag = new byte[TagSizeInBytes];
            byte[] ciphertext = new byte[plaintext.Length];

            // 使用新的构造函数，明确指定标签大小
            using var aesGcm = new AesGcm(key, TagSizeInBytes); // 转换为比特
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            // 组合结果: nonce + ciphertext + tag
            var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

            return result;
        }

        /// <summary>
        /// 使用AES-GCM解密（符合.NET 8.0 API）
        /// </summary>
        public static byte[] DecryptWithAesGcm(byte[] encryptedData, byte[] key, byte[] associatedData = null)
        {
            // 解析数据: nonce + ciphertext + tag
            byte[] nonce = new byte[12];
            byte[] ciphertext = new byte[encryptedData.Length - nonce.Length - TagSizeInBytes];
            byte[] tag = new byte[TagSizeInBytes];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(encryptedData, nonce.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(encryptedData, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

            byte[] plaintext = new byte[ciphertext.Length];

            // 使用新的构造函数，明确指定标签大小
            using var aesGcm = new AesGcm(key, TagSizeInBytes); // 转换为比特
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

            return plaintext;
        }

        /// <summary>
        /// 使用私钥签名
        /// </summary>
        public byte[] SignData(byte[] data)
        {
            return _identityKey.SignData(data, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// 验证签名
        /// </summary>
        public static bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// 生成随机盐
        /// </summary>
        public static byte[] GenerateRandomSalt(int length = 32)
        {
            byte[] salt = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        public void Dispose()
        {
            _ephemeralKeyPair?.Dispose();
            _identityKey?.Dispose();
        }
    }
}
