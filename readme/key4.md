您遇到了 .NET 8.0 中 `AesGcm` 构造函数的 API 变更。让我为您提供更新后的正确实现：

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
        private const int TagSizeInBytes = 16; // 128-bit authentication tag

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
            using var aesGcm = new AesGcm(key, TagSizeInBytes * 8); // 转换为比特
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
            using var aesGcm = new AesGcm(key, TagSizeInBytes * 8); // 转换为比特
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
            public byte[] Salt { get; set; }
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
            var salt = SecureEcdhService.GenerateRandomSalt();
            
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
            return SecureEcdhService.VerifySignature(dataToVerify, message.Signature, message.IdentityPublicKey);
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

    // 使用示例
    class Program
    {
        static void Main(string[] args)
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
            var encrypted = SecureEcdhService.EncryptWithAesGcm(plaintext, testKey, associatedData);
            Console.WriteLine($"加密完成，数据长度: {encrypted.Length} 字节");

            // 解密
            var decrypted = SecureEcdhService.DecryptWithAesGcm(encrypted, testKey, associatedData);
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
            using var alice = new SecureEcdhService();
            using var bob = new SecureEcdhService();

            // Alice 创建密钥交换消息
            var aliceMessage = CompleteKeyExchangeProtocol.CreateKeyExchangeMessage(alice);
            Console.WriteLine("Alice创建密钥交换消息");

            // Bob 验证Alice的消息
            if (!CompleteKeyExchangeProtocol.VerifyKeyExchangeMessage(aliceMessage))
            {
                throw new Exception("Alice消息验证失败");
            }
            Console.WriteLine("Bob验证Alice消息成功");

            // Bob 创建密钥交换消息
            var bobMessage = CompleteKeyExchangeProtocol.CreateKeyExchangeMessage(bob);
            Console.WriteLine("Bob创建密钥交换消息");

            // Alice 验证Bob的消息
            if (!CompleteKeyExchangeProtocol.VerifyKeyExchangeMessage(bobMessage))
            {
                throw new Exception("Bob消息验证失败");
            }
            Console.WriteLine("Alice验证Bob消息成功");

            // 双方派生共享密钥
            var aliceSharedSecret = alice.DeriveSharedSecret(bobMessage.EphemeralPublicKey);
            var bobSharedSecret = bob.DeriveSharedSecret(aliceMessage.EphemeralPublicKey);

            // 使用相同的盐派生AES密钥
            var aliceAesKey = SecureEcdhService.DeriveAes256Key(aliceSharedSecret, aliceMessage.Salt);
            var bobAesKey = SecureEcdhService.DeriveAes256Key(bobSharedSecret, aliceMessage.Salt);

            Console.WriteLine($"Alice AES密钥: {BitConverter.ToString(aliceAesKey).Substring(0, 32)}...");
            Console.WriteLine($"Bob AES密钥: {BitConverter.ToString(bobAesKey).Substring(0, 32)}...");

            // 验证密钥匹配
            bool keysMatch = CryptographicOperations.FixedTimeEquals(aliceAesKey, bobAesKey);
            Console.WriteLine($"密钥匹配验证: {keysMatch}");

            // 测试通信
            var testMessage = "密钥交换成功！";
            var encrypted = SecureEcdhService.EncryptWithAesGcm(
                Encoding.UTF8.GetBytes(testMessage), 
                aliceAesKey
            );
            
            var decrypted = SecureEcdhService.DecryptWithAesGcm(encrypted, bobAesKey);
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
```

## 关键修复点

### 1. **修复 AesGcm 构造函数**

```csharp
// 过时的用法（不要使用）：
// using var aesGcm = new AesGcm(key);

// 正确的用法（.NET 8.0+）：
using var aesGcm = new AesGcm(key, TagSizeInBytes * 8); // 明确指定标签大小（比特）
```

### 2. **标签大小常量**

```csharp
private const int TagSizeInBytes = 16; // 128-bit authentication tag
```

### 3. **AesGcmHelper 包装类**

```csharp
public class AesGcmHelper : IDisposable
{
    private readonly AesGcm _aesGcm;
    private readonly int _tagSizeInBytes;

    public AesGcmHelper(byte[] key, int tagSizeInBits = 128)
    {
        _tagSizeInBytes = tagSizeInBits / 8;
        _aesGcm = new AesGcm(key, tagSizeInBits); // 明确指定标签大小
    }
}
```

## 推荐的标签大小

在 .NET 8.0 中，`AesGcm` 支持以下标签大小：

- **128位（16字节）** - **推荐**，提供最佳安全性
- **96位（12字节）** - 可接受，但安全性稍低
- **64位（8字节）** - 不推荐用于生产环境

## 生产环境建议

### 1. **始终使用128位标签**

```csharp
// 生产环境代码
using var aesGcm = new AesGcm(key, 128); // 128位标签
```

### 2. **密钥派生最佳实践**

```csharp
public static byte[] DeriveAes256Key(byte[] sharedSecret, byte[] salt, byte[] info)
{
    // 使用HKDF安全派生密钥
    // 包含盐和信息参数确保密钥分离
}
```

### 3. **错误处理**

```csharp
try
{
    var decrypted = SecureEcdhService.DecryptWithAesGcm(encryptedData, key);
    // 处理解密数据
}
catch (CryptographicException ex)
{
    // 处理认证失败等情况
    Console.WriteLine($"解密失败: {ex.Message}");
}
```

这个修复后的实现完全符合 .NET 8.0 的 API 要求，同时保持了最佳的安全实践。所有 `AesGcm` 实例现在都明确指定了标签大小，消除了过时警告。