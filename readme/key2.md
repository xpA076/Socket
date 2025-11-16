您遇到这个问题是因为在 .NET 中，GCM 模式不是通过 `CipherMode` 枚举来设置的，而是通过专门的 `AesGcm` 类来实现的。让我为您提供正确的实现：

## 问题原因

在 .NET 中：
- **CBC、ECB、CFB** 等模式通过 `Aes` 类的 `Mode` 属性设置
- **GCM** 模式通过专门的 `System.Security.Cryptography.AesGcm` 类实现

## 修正后的代码

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecureECDHWithGCM
{
    public class SecureEcdhService : IDisposable
    {
        private readonly ECDiffieHellman _ephemeralKeyPair;
        private readonly ECDsa _identityKey;

        public SecureEcdhService()
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
        /// 使用AES-GCM加密
        /// </summary>
        public static byte[] EncryptWithAesGcm(byte[] plaintext, byte[] key, byte[] associatedData = null)
        {
            // GCM推荐使用12字节的nonce
            byte[] nonce = new byte[12];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);

            // 创建认证标签
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[plaintext.Length];

            using var aesGcm = new AesGcm(key);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            // 组合结果: nonce + ciphertext + tag
            var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

            return result;
        }

        /// <summary>
        /// 使用AES-GCM解密
        /// </summary>
        public static byte[] DecryptWithAesGcm(byte[] encryptedData, byte[] key, byte[] associatedData = null)
        {
            // 解析数据: nonce + ciphertext + tag
            byte[] nonce = new byte[12];
            byte[] ciphertext = new byte[encryptedData.Length - nonce.Length - 16];
            byte[] tag = new byte[16];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(encryptedData, nonce.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(encryptedData, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

            byte[] plaintext = new byte[ciphertext.Length];

            using var aesGcm = new AesGcm(key);
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

        public void Dispose()
        {
            _ephemeralKeyPair?.Dispose();
            _identityKey?.Dispose();
        }
    }

    /// <summary>
    /// 完整的密钥交换协议
    /// </summary>
    public class CompleteKeyExchangeProtocol
    {
        public class KeyExchangeMessage
        {
            public byte[] EphemeralPublicKey { get; set; }
            public byte[] IdentityPublicKey { get; set; }
            public byte[] Signature { get; set; }
            public byte[] Timestamp { get; set; }
        }

        public class EncryptedMessage
        {
            public byte[] Data { get; set; }
            public byte[] AssociatedData { get; set; }
        }

        /// <summary>
        /// 创建密钥交换消息
        /// </summary>
        public static KeyExchangeMessage CreateKeyExchangeMessage(SecureEcdhService party)
        {
            var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var ephemeralPublicKey = party.GetPublicKey();
            
            // 创建签名数据
            var dataToSign = CombineData(ephemeralPublicKey, timestamp);
            var signature = party.SignData(dataToSign);

            return new KeyExchangeMessage
            {
                EphemeralPublicKey = ephemeralPublicKey,
                IdentityPublicKey = party.GetIdentityPublicKey(),
                Signature = signature,
                Timestamp = timestamp
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
            var dataToVerify = CombineData(message.EphemeralPublicKey, message.Timestamp);
            return SecureEcdhService.VerifySignature(dataToVerify, message.Signature, message.IdentityPublicKey);
        }

        private static byte[] CombineData(byte[] publicKey, byte[] timestamp)
        {
            using var ms = new MemoryStream();
            ms.Write(publicKey, 0, publicKey.Length);
            ms.Write(timestamp, 0, timestamp.Length);
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

    /// <summary>
    /// 密钥派生工具
    /// </summary>
    public static class KeyDerivation
    {
        public static byte[] DeriveAesKey(byte[] sharedSecret, string purpose = "encryption")
        {
            // 使用HMAC-based KDF
            using var hmac = new HMACSHA256(sharedSecret);
            var purposeBytes = Encoding.UTF8.GetBytes(purpose);
            return hmac.ComputeHash(purposeBytes);
        }
    }

    // 使用示例
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== .NET 8.0 ECDH + AES-GCM 完整演示 ===\n");

                // 创建通信双方
                using var alice = new SecureEcdhService();
                using var bob = new SecureEcdhService();

                Console.WriteLine("1. 通信双方初始化完成");

                // Alice 创建密钥交换消息
                var aliceMessage = CompleteKeyExchangeProtocol.CreateKeyExchangeMessage(alice);
                Console.WriteLine("2. Alice创建密钥交换消息");

                // Bob 验证Alice的消息
                if (!CompleteKeyExchangeProtocol.VerifyKeyExchangeMessage(aliceMessage))
                {
                    throw new Exception("Alice消息验证失败");
                }
                Console.WriteLine("3. Bob验证Alice消息成功");

                // Bob 创建密钥交换消息
                var bobMessage = CompleteKeyExchangeProtocol.CreateKeyExchangeMessage(bob);
                Console.WriteLine("4. Bob创建密钥交换消息");

                // Alice 验证Bob的消息
                if (!CompleteKeyExchangeProtocol.VerifyKeyExchangeMessage(bobMessage))
                {
                    throw new Exception("Bob消息验证失败");
                }
                Console.WriteLine("5. Alice验证Bob消息成功");

                // 双方派生共享密钥
                var aliceSharedSecret = alice.DeriveSharedSecret(bobMessage.EphemeralPublicKey);
                var bobSharedSecret = bob.DeriveSharedSecret(aliceMessage.EphemeralPublicKey);

                Console.WriteLine("6. 双方派生共享密钥完成");

                // 派生AES密钥
                var aliceAesKey = KeyDerivation.DeriveAesKey(aliceSharedSecret);
                var bobAesKey = KeyDerivation.DeriveAesKey(bobSharedSecret);

                Console.WriteLine($"Alice AES密钥: {BitConverter.ToString(aliceAesKey).Substring(0, 32)}...");
                Console.WriteLine($"Bob AES密钥: {BitConverter.ToString(bobAesKey).Substring(0, 32)}...");

                // 测试加密通信
                TestSecureCommunication(aliceAesKey, bobAesKey);

                Console.WriteLine("\n=== 演示完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        static void TestSecureCommunication(byte[] aliceKey, byte[] bobKey)
        {
            Console.WriteLine("\n--- 安全通信测试 ---");

            var originalMessage = "这是一条使用ECDH和AES-GCM加密的秘密消息！";
            Console.WriteLine($"原始消息: {originalMessage}");

            // Alice 加密消息
            var plaintext = Encoding.UTF8.GetBytes(originalMessage);
            var associatedData = Encoding.UTF8.GetBytes("元数据验证"); // 可选的关联数据
            
            var encrypted = SecureEcdhService.EncryptWithAesGcm(plaintext, aliceKey, associatedData);
            Console.WriteLine($"加密完成，数据长度: {encrypted.Length} 字节");

            // Bob 解密消息
            var decrypted = SecureEcdhService.DecryptWithAesGcm(encrypted, bobKey, associatedData);
            var decryptedMessage = Encoding.UTF8.GetString(decrypted);
            Console.WriteLine($"解密消息: {decryptedMessage}");

            // 验证通信成功
            Console.WriteLine($"通信成功: {originalMessage == decryptedMessage}");

            // 测试关联数据验证
            TestAssociatedDataVerification(aliceKey, bobKey);
        }

        static void TestAssociatedDataVerification(byte[] aliceKey, byte[] bobKey)
        {
            Console.WriteLine("\n--- 关联数据验证测试 ---");

            var message = "需要验证关联数据的消息";
            var correctAssociatedData = Encoding.UTF8.GetBytes("正确的关联数据");
            var wrongAssociatedData = Encoding.UTF8.GetBytes("错误的关联数据");

            // 使用正确的关联数据加密
            var encrypted = SecureEcdhService.EncryptWithAesGcm(
                Encoding.UTF8.GetBytes(message), 
                aliceKey, 
                correctAssociatedData
            );

            try
            {
                // 使用错误的关联数据解密（应该失败）
                SecureEcdhService.DecryptWithAesGcm(encrypted, bobKey, wrongAssociatedData);
                Console.WriteLine("❌ 关联数据验证失败：应该抛出异常");
            }
            catch (CryptographicException)
            {
                Console.WriteLine("✓ 关联数据验证成功：错误的关联数据导致解密失败");
            }

            try
            {
                // 使用正确的关联数据解密（应该成功）
                var decrypted = SecureEcdhService.DecryptWithAesGcm(encrypted, bobKey, correctAssociatedData);
                Console.WriteLine($"✓ 正确关联数据解密成功: {Encoding.UTF8.GetString(decrypted)}");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"❌ 正确关联数据解密失败: {ex.Message}");
            }
        }
    }
}
```

## 关键修正点

### 1. **使用正确的AES-GCM实现**

```csharp
// 错误的方式（不存在）
aes.Mode = CipherMode.GCM;

// 正确的方式
using var aesGcm = new AesGcm(key);
aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
```

### 2. **AES-GCM参数说明**

- **Nonce**：12字节，不需要保密但不能重复使用
- **Tag**：16字节，认证标签
- **Associated Data**：可选，用于认证但不加密的数据

### 3. **数据组合格式**

```csharp
// 加密结果组合：Nonce + Ciphertext + Tag
var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);
```

## 在 .NET 8.0 中的兼容性

这个实现在 .NET 8.0 中完全兼容，因为：
- `AesGcm` 类从 .NET Core 3.0 开始可用
- `ECDiffieHellman` 和 `ECDsa` 在 .NET 8.0 中稳定可靠
- 所有使用的加密原语都是 .NET 标准库的一部分

## 其他可用的认证加密模式

如果您需要其他选项，.NET 8.0 还提供：

```csharp
// AES-CCM模式（如果可用）
using var aesCcm = new AesCcm(key);

// ChaCha20Poly1305（如果平台支持）
using var chacha = new ChaCha20Poly1305(key);
```

这个修正后的实现应该在您的 VS2022 .NET 8.0 环境中正常工作，提供了完整的ECDH密钥交换和AES-GCM加密功能。