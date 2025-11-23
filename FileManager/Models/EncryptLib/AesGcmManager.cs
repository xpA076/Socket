using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.EncryptLib
{
    public class AesGcmManager
    {
        private const int TagSizeInBytes = 16; // 128-bit authentication tag

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

            try
            {
                // 使用新的构造函数，明确指定标签大小
                using var aesGcm = new AesGcm(key, TagSizeInBytes); // 转换为比特
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
                return plaintext;
            }
            catch (CryptographicException)
            {
                throw;
            }
        }
    }
}
