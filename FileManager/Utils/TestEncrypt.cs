using FileManager.Models.EncryptLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils
{
    public class TestEncrypt
    {
        /// <summary>
        /// 支持不同标签大小的AES-GCM包装类
        /// </summary>
        public class AesGcmHelper : IDisposable
        {
            private readonly AesGcm _aesGcm;
            private readonly int _tagSizeInBytes;

            public AesGcmHelper(byte[] key, int tagSizeInBits = 128)
            {
                if (tagSizeInBits != 128 && tagSizeInBits != 96 && tagSizeInBits != 64)
                    throw new ArgumentException("标签大小必须是128、96或64位", nameof(tagSizeInBits));

                _tagSizeInBytes = tagSizeInBits / 8;
                _aesGcm = new AesGcm(key, tagSizeInBits);
            }

            /// <summary>
            /// 加密数据
            /// </summary>
            public byte[] Encrypt(byte[] plaintext, byte[] nonce = null, byte[] associatedData = null)
            {
                nonce ??= GenerateNonce();
                byte[] tag = new byte[_tagSizeInBytes];
                byte[] ciphertext = new byte[plaintext.Length];

                _aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

                // 组合结果: nonce + ciphertext + tag
                var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
                Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
                Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

                return result;
            }

            /// <summary>
            /// 解密数据
            /// </summary>
            public byte[] Decrypt(byte[] encryptedData, byte[] associatedData = null)
            {
                // 解析数据: nonce + ciphertext + tag
                byte[] nonce = new byte[12];
                byte[] ciphertext = new byte[encryptedData.Length - nonce.Length - _tagSizeInBytes];
                byte[] tag = new byte[_tagSizeInBytes];

                Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);
                Buffer.BlockCopy(encryptedData, nonce.Length, ciphertext, 0, ciphertext.Length);
                Buffer.BlockCopy(encryptedData, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

                byte[] plaintext = new byte[ciphertext.Length];
                _aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

                return plaintext;
            }

            /// <summary>
            /// 生成随机nonce
            /// </summary>
            public static byte[] GenerateNonce(int size = 12)
            {
                byte[] nonce = new byte[size];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(nonce);
                return nonce;
            }

            public void Dispose()
            {
                _aesGcm?.Dispose();
            }
        }

        public void Test1()
        {
            try
            {
                Console.WriteLine("=== .NET 8.0 ECDH + AES-GCM (修复版本) ===\n");

                // 演示修复后的AES-GCM使用
                DemoFixedAesGcm();

                // 完整的密钥交换演示
                DemoCompleteKeyExchange();

                // 演示不同标签大小
                DemoDifferentTagSizes();

                Console.WriteLine("\n=== 所有演示完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        static void DemoFixedAesGcm()
        {
            Console.WriteLine("1. 修复后的AES-GCM演示");
            Console.WriteLine("------------------------");

            // 生成测试密钥
            byte[] testKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(testKey);
            }

            var originalMessage = "这是一条测试消息！";
            var plaintext = Encoding.UTF8.GetBytes(originalMessage);
            var associatedData = Encoding.UTF8.GetBytes("验证数据");

            // 使用修复后的方法加密
            var encrypted = EcdhManager.EncryptWithAesGcm(plaintext, testKey, associatedData);
            Console.WriteLine($"加密完成，数据长度: {encrypted.Length} 字节");

            // 解密
            var decrypted = EcdhManager.DecryptWithAesGcm(encrypted, testKey, associatedData);
            var decryptedMessage = Encoding.UTF8.GetString(decrypted);
            Console.WriteLine($"解密消息: {decryptedMessage}");

            // 验证
            Console.WriteLine($"加解密验证: {originalMessage == decryptedMessage}");
            Console.WriteLine();
        }

        static void DemoCompleteKeyExchange()
        {
            Console.WriteLine("2. 完整密钥交换演示");
            Console.WriteLine("---------------------");

            // 创建通信双方
            using var alice = new EcdhManager();
            using var bob = new EcdhManager();

            // Alice 创建密钥交换消息
            var aliceMessage = EcdhKeyExchangeProtocol.CreateKeyExchangeMessage(alice);
            Console.WriteLine("Alice创建密钥交换消息");

            // Bob 验证Alice的消息
            if (!EcdhKeyExchangeProtocol.VerifyKeyExchangeMessage(aliceMessage))
            {
                throw new Exception("Alice消息验证失败");
            }
            Console.WriteLine("Bob验证Alice消息成功");

            // Bob 创建密钥交换消息
            var bobMessage = EcdhKeyExchangeProtocol.CreateKeyExchangeMessage(bob);
            Console.WriteLine("Bob创建密钥交换消息");

            // Alice 验证Bob的消息
            if (!EcdhKeyExchangeProtocol.VerifyKeyExchangeMessage(bobMessage))
            {
                throw new Exception("Bob消息验证失败");
            }
            Console.WriteLine("Alice验证Bob消息成功");

            // 双方派生共享密钥
            var aliceSharedSecret = alice.DeriveSharedSecret(bobMessage.EphemeralPublicKey);
            var bobSharedSecret = bob.DeriveSharedSecret(aliceMessage.EphemeralPublicKey);

            // 使用相同的盐派生AES密钥
            var aliceAesKey = EcdhManager.DeriveAes256Key(aliceSharedSecret, aliceMessage.Salt);
            var bobAesKey = EcdhManager.DeriveAes256Key(bobSharedSecret, aliceMessage.Salt);

            Console.WriteLine($"Alice AES密钥: {BitConverter.ToString(aliceAesKey).Substring(0, 32)}...");
            Console.WriteLine($"Bob AES密钥: {BitConverter.ToString(bobAesKey).Substring(0, 32)}...");

            // 验证密钥匹配
            bool keysMatch = CryptographicOperations.FixedTimeEquals(aliceAesKey, bobAesKey);
            Console.WriteLine($"密钥匹配验证: {keysMatch}");

            // 测试通信
            var testMessage = "密钥交换成功！";
            var encrypted = EcdhManager.EncryptWithAesGcm(
                Encoding.UTF8.GetBytes(testMessage),
                aliceAesKey
            );

            var decrypted = EcdhManager.DecryptWithAesGcm(encrypted, bobAesKey);
            Console.WriteLine($"测试通信: {Encoding.UTF8.GetString(decrypted)}");
            Console.WriteLine();
        }

        static void DemoDifferentTagSizes()
        {
            Console.WriteLine("3. 不同标签大小演示");
            Console.WriteLine("---------------------");

            byte[] testKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(testKey);
            }

            var testData = Encoding.UTF8.GetBytes("测试不同标签大小");

            // 测试128位标签（默认，最安全）
            using var gcm128 = new AesGcmHelper(testKey, 128);
            var encrypted128 = gcm128.Encrypt(testData);
            var decrypted128 = gcm128.Decrypt(encrypted128);
            Console.WriteLine($"128位标签: {Encoding.UTF8.GetString(decrypted128)}");

            // 测试96位标签
            using var gcm96 = new AesGcmHelper(testKey, 96);
            var encrypted96 = gcm96.Encrypt(testData);
            var decrypted96 = gcm96.Decrypt(encrypted96);
            Console.WriteLine($"96位标签: {Encoding.UTF8.GetString(decrypted96)}");

            // 注意：64位标签安全性较低，不推荐在生产环境使用
            try
            {
                using var gcm64 = new AesGcmHelper(testKey, 64);
                var encrypted64 = gcm64.Encrypt(testData);
                var decrypted64 = gcm64.Decrypt(encrypted64);
                Console.WriteLine($"64位标签: {Encoding.UTF8.GetString(decrypted64)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"64位标签错误: {ex.Message}");
            }

            Console.WriteLine("注意：128位标签提供最佳安全性，推荐在生产环境使用");
        }
    }
}

