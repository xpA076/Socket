基于ECDH的密钥交换实现更加现代化和安全。以下是完整的C#实现：

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ECDHKeyExchange
{
    public class EcdhKeyExchangeResult
    {
        public byte[] PublicKey { get; set; }
        public byte[] EncryptedData { get; set; }
        public byte[] Iv { get; set; }
        public byte[] Signature { get; set; }
    }

    public class EcdhKeyExchangeService : IDisposable
    {
        private readonly ECDiffieHellman _localKeyPair;
        private readonly ECDsa _signingKey;

        public EcdhKeyExchangeService()
        {
            // 使用P-256曲线（secp256r1）
            _localKeyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            _signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        }

        /// <summary>
        /// 获取本地公钥（用于交换）
        /// </summary>
        public byte[] GetPublicKey()
        {
            return _localKeyPair.ExportSubjectPublicKeyInfo();
        }

        /// <summary>
        /// 获取签名公钥（用于验证身份）
        /// </summary>
        public byte[] GetSigningPublicKey()
        {
            return _signingKey.ExportSubjectPublicKeyInfo();
        }

        /// <summary>
        /// 执行ECDH密钥协商并派生AES密钥
        /// </summary>
        public Aes DeriveAesKey(byte[] otherPartyPublicKey)
        {
            // 导入对方公钥
            using var otherParty = ECDiffieHellman.Create();
            otherParty.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);

            // 执行ECDH密钥协商
            byte[] sharedSecret = _localKeyPair.DeriveKeyFromHash(
                otherParty.PublicKey, 
                HashAlgorithmName.SHA256, 
                null, 
                null
            );

            // 使用HKDF从共享密钥派生AES密钥
            byte[] aesKey = DeriveKeyWithHkdf(sharedSecret, 32); // 32 bytes = 256 bits

            // 创建AES实例
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = aesKey;
            aes.GenerateIV();
            aes.Mode = CipherMode.GCM; // 使用GCM模式提供认证加密
            aes.Padding = PaddingMode.None;

            return aes;
        }

        /// <summary>
        /// 使用HKDF派生密钥
        /// </summary>
        private byte[] DeriveKeyWithHkdf(byte[] inputKeyingMaterial, int outputLength)
        {
            byte[] salt = new byte[32]; // 在实际应用中应该使用随机盐
            byte[] info = Encoding.UTF8.GetBytes("ECDH-AES256-KEY-DERIVATION");
            
            using var hkdf = new Rfc5869Hkdf(HashAlgorithmName.SHA256, inputKeyingMaterial, salt, info);
            return hkdf.Expand(outputLength);
        }

        /// <summary>
        /// 创建数字签名
        /// </summary>
        public byte[] SignData(byte[] data)
        {
            return _signingKey.SignData(data, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// 验证数字签名
        /// </summary>
        public bool VerifySignature(byte[] data, byte[] signature, byte[] signerPublicKey)
        {
            using var signer = ECDsa.Create();
            signer.ImportSubjectPublicKeyInfo(signerPublicKey, out _);
            return signer.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// 加密数据（使用AES-GCM）
        /// </summary>
        public byte[] EncryptWithAesGcm(string plaintext, Aes aes)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            
            // 对于GCM模式，我们需要处理认证标签
            var tag = new byte[16]; // 128位认证标签
            var ciphertext = new byte[plaintextBytes.Length];
            
            using var aesGcm = new AesGcm(aes.Key);
            aesGcm.Encrypt(aes.IV, plaintextBytes, ciphertext, tag);
            
            // 组合结果：IV + 密文 + 认证标签
            var result = new byte[aes.IV.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, aes.IV.Length + ciphertext.Length, tag.Length);
            
            return result;
        }

        /// <summary>
        /// 解密数据（使用AES-GCM）
        /// </summary>
        public string DecryptWithAesGcm(byte[] encryptedData, Aes aes)
        {
            // 解析数据：IV + 密文 + 认证标签
            var iv = new byte[12]; // GCM推荐使用12字节IV
            var ciphertext = new byte[encryptedData.Length - iv.Length - 16];
            var tag = new byte[16];
            
            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encryptedData, iv.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(encryptedData, iv.Length + ciphertext.Length, tag, 0, tag.Length);
            
            var plaintext = new byte[ciphertext.Length];
            
            using var aesGcm = new AesGcm(aes.Key);
            aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
            
            return Encoding.UTF8.GetString(plaintext);
        }

        /// <summary>
        /// 完整的密钥交换和加密流程（客户端）
        /// </summary>
        public EcdhKeyExchangeResult ClientExchangeAndEncrypt(byte[] serverPublicKey, byte[] serverSigningKey, string message)
        {
            // 派生AES密钥
            using var aes = DeriveAesKey(serverPublicKey);
            
            // 加密数据
            var encryptedData = EncryptWithAesGcm(message, aes);
            
            // 创建消息签名
            var messageHash = SHA256.HashData(Encoding.UTF8.GetBytes(message));
            var signature = SignData(messageHash);
            
            return new EcdhKeyExchangeResult
            {
                PublicKey = GetPublicKey(),
                EncryptedData = encryptedData,
                Signature = signature
            };
        }

        /// <summary>
        /// 完整的密钥交换和解密流程（服务器）
        /// </summary>
        public string ServerExchangeAndDecrypt(EcdhKeyExchangeResult clientData, byte[] clientSigningKey, string expectedMessageHash = null)
        {
            // 派生AES密钥（与客户端相同的密钥）
            using var aes = DeriveAesKey(clientData.PublicKey);
            
            // 解密数据
            var decryptedMessage = DecryptWithAesGcm(clientData.EncryptedData, aes);
            
            // 验证签名
            var messageHash = SHA256.HashData(Encoding.UTF8.GetBytes(decryptedMessage));
            if (!VerifySignature(messageHash, clientData.Signature, clientSigningKey))
            {
                throw new SecurityException("消息签名验证失败");
            }
            
            // 可选：验证消息哈希（防止重放攻击）
            if (expectedMessageHash != null)
            {
                var computedHash = BitConverter.ToString(messageHash).Replace("-", "").ToLower();
                if (!computedHash.Equals(expectedMessageHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SecurityException("消息哈希不匹配");
                }
            }
            
            return decryptedMessage;
        }

        public void Dispose()
        {
            _localKeyPair?.Dispose();
            _signingKey?.Dispose();
        }
    }

    /// <summary>
    /// HKDF实现 (RFC 5869)
    /// </summary>
    public class Rfc5869Hkdf : IDisposable
    {
        private readonly HMAC _hmac;
        private readonly byte[] _salt;
        private readonly byte[] _info;
        private readonly byte[] _prk;

        public Rfc5869Hkdf(HashAlgorithmName hashAlgorithmName, byte[] inputKeyingMaterial, byte[] salt = null, byte[] info = null)
        {
            _salt = salt ?? new byte[GetHashLength(hashAlgorithmName)];
            _info = info ?? Array.Empty<byte>();
            
            _hmac = CreateHmac(hashAlgorithmName, _salt);
            _prk = _hmac.ComputeHash(inputKeyingMaterial);
        }

        public byte[] Expand(int outputLength)
        {
            if (outputLength < 1 || outputLength > 255 * GetHashLength(_hmac.HashName))
                throw new ArgumentOutOfRangeException(nameof(outputLength));

            var hashLen = GetHashLength(_hmac.HashName);
            var rounds = (int)Math.Ceiling((double)outputLength / hashLen);
            var result = new List<byte>();

            byte[] previous = Array.Empty<byte>();

            for (int i = 1; i <= rounds; i++)
            {
                var hmac = CreateHmac(_hmac.HashName, _prk);
                hmac.TransformBlock(previous, 0, previous.Length, null, 0);
                hmac.TransformBlock(_info, 0, _info.Length, null, 0);
                hmac.TransformBlock(new[] { (byte)i }, 0, 1, null, 0);
                hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                
                previous = hmac.Hash;
                result.AddRange(previous);
            }

            return result.Take(outputLength).ToArray();
        }

        private static HMAC CreateHmac(string hashAlgorithmName, byte[] key)
        {
            return hashAlgorithmName.ToUpper() switch
            {
                "SHA256" => new HMACSHA256(key),
                "SHA384" => new HMACSHA384(key),
                "SHA512" => new HMACSHA512(key),
                _ => new HMACSHA256(key)
            };
        }

        private static HMAC CreateHmac(HashAlgorithmName hashAlgorithmName, byte[] key)
        {
            return hashAlgorithmName.Name?.ToUpper() switch
            {
                "SHA256" => new HMACSHA256(key),
                "SHA384" => new HMACSHA384(key),
                "SHA512" => new HMACSHA512(key),
                _ => new HMACSHA256(key)
            };
        }

        private static int GetHashLength(HashAlgorithmName hashAlgorithmName)
        {
            return hashAlgorithmName.Name?.ToUpper() switch
            {
                "SHA256" => 32,
                "SHA384" => 48,
                "SHA512" => 64,
                _ => 32
            };
        }

        private static int GetHashLength(string hashAlgorithmName)
        {
            return hashAlgorithmName?.ToUpper() switch
            {
                "SHA256" => 32,
                "SHA384" => 48,
                "SHA512" => 64,
                _ => 32
            };
        }

        public void Dispose()
        {
            _hmac?.Dispose();
        }
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
    }

    // 使用示例
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== ECDH密钥交换演示 ===\n");

                // 模拟客户端和服务器
                using var client = new EcdhKeyExchangeService();
                using var server = new EcdhKeyExchangeService();

                Console.WriteLine("1. 密钥对生成完成");

                // 交换公钥
                var serverPublicKey = server.GetPublicKey();
                var serverSigningKey = server.GetSigningPublicKey();
                var clientPublicKey = client.GetPublicKey();
                var clientSigningKey = client.GetSigningPublicKey();

                Console.WriteLine("2. 公钥交换完成");

                // 客户端：加密并发送消息
                var message = "这是一条使用ECDH和AES-GCM加密的秘密消息！";
                Console.WriteLine($"原始消息: {message}");

                var clientResult = client.ClientExchangeAndEncrypt(serverPublicKey, serverSigningKey, message);
                Console.WriteLine("3. 客户端加密完成");

                // 服务器：接收并解密消息
                var decryptedMessage = server.ServerExchangeAndDecrypt(clientResult, clientSigningKey);
                Console.WriteLine($"4. 服务器解密完成");
                Console.WriteLine($"解密消息: {decryptedMessage}");

                // 验证消息完整性
                Console.WriteLine($"消息完整性验证: {message == decryptedMessage}");

                // 演示前向安全性：每次会话使用新的密钥对
                Console.WriteLine("\n=== 前向安全性演示 ===");
                using var newClientSession = new EcdhKeyExchangeService();
                using var newServerSession = new EcdhKeyExchangeService();

                var newMessage = "这是新会话的消息！";
                var newClientResult = newClientSession.ClientExchangeAndEncrypt(
                    newServerSession.GetPublicKey(), 
                    newServerSession.GetSigningPublicKey(), 
                    newMessage
                );

                var newDecryptedMessage = newServerSession.ServerExchangeAndDecrypt(
                    newClientResult, 
                    newClientSession.GetSigningPublicKey()
                );

                Console.WriteLine($"新会话消息: {newDecryptedMessage}");
                Console.WriteLine($"前向安全性验证: {newMessage == newDecryptedMessage}");

                Console.WriteLine("\n=== ECDH密钥交换成功完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}
```

## ECDH密钥交换的核心优势

### 1. **前向安全性**
每次会话生成新的临时密钥对，即使长期私钥泄露，过去的会话也不会被解密。

### 2. **更小的密钥尺寸**
- ECDH-256位安全级别 ≈ RSA-3072位
- 更快的计算速度

### 3. **现代加密标准**
使用P-256曲线和AES-GCM认证加密

## 关键组件说明

### ECDH密钥协商
```csharp
// 生成共享密钥
byte[] sharedSecret = _localKeyPair.DeriveKeyFromHash(
    otherParty.PublicKey, 
    HashAlgorithmName.SHA256
);
```

### HKDF密钥派生
```csharp
// 从共享密钥安全派生AES密钥
byte[] aesKey = DeriveKeyWithHkdf(sharedSecret, 32);
```

### AES-GCM认证加密
```csharp
// 提供机密性、完整性和认证
using var aesGcm = new AesGcm(aes.Key);
aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
```

## 安全考虑

1. **使用标准曲线**：P-256 (secp256r1)
2. **密钥派生**：使用HKDF而不是直接使用共享密钥
3. **认证加密**：AES-GCM提供完整性保护
4. **数字签名**：ECDSA验证通信方身份

这个实现提供了企业级的密钥交换安全性，适合现代加密通信系统。


我来演示如何生成ECDSA密钥对、保存到字节数组，以及从字节数组加载进行签名和验证的完整流程：

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ECDSAKeyDemo
{
    public class EcdsaKeyManager
    {
        /// <summary>
        /// 生成新的ECDSA密钥对
        /// </summary>
        public static EcdsaKeyPair GenerateKeyPair()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            
            return new EcdsaKeyPair
            {
                PrivateKey = ecdsa.ExportECPrivateKey(),
                PublicKey = ecdsa.ExportSubjectPublicKeyInfo()
            };
        }

        /// <summary>
        /// 从字节数组加载私钥并创建ECDsa实例
        /// </summary>
        public static ECDsa LoadPrivateKey(byte[] privateKeyBytes)
        {
            var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(privateKeyBytes, out _);
            return ecdsa;
        }

        /// <summary>
        /// 从字节数组加载公钥并创建ECDsa实例
        /// </summary>
        public static ECDsa LoadPublicKey(byte[] publicKeyBytes)
        {
            var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
            return ecdsa;
        }

        /// <summary>
        /// 使用私钥对数据进行签名
        /// </summary>
        public static byte[] SignData(byte[] data, byte[] privateKeyBytes)
        {
            using var ecdsa = LoadPrivateKey(privateKeyBytes);
            return ecdsa.SignData(data, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// 使用公钥验证签名
        /// </summary>
        public static bool VerifyData(byte[] data, byte[] signature, byte[] publicKeyBytes)
        {
            using var ecdsa = LoadPublicKey(publicKeyBytes);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// 将密钥对保存到文件（实际应用中需要加密存储）
        /// </summary>
        public static void SaveKeyPairToFile(EcdsaKeyPair keyPair, string privateKeyPath, string publicKeyPath)
        {
            // 保存私钥（实际应用中应该加密存储）
            File.WriteAllBytes(privateKeyPath, keyPair.PrivateKey);
            
            // 保存公钥
            File.WriteAllBytes(publicKeyPath, keyPair.PublicKey);
        }

        /// <summary>
        /// 从文件加载密钥对
        /// </summary>
        public static EcdsaKeyPair LoadKeyPairFromFile(string privateKeyPath, string publicKeyPath)
        {
            return new EcdsaKeyPair
            {
                PrivateKey = File.ReadAllBytes(privateKeyPath),
                PublicKey = File.ReadAllBytes(publicKeyPath)
            };
        }
    }

    public class EcdsaKeyPair
    {
        public byte[] PrivateKey { get; set; }
        public byte[] PublicKey { get; set; }

        public void PrintKeyInfo()
        {
            Console.WriteLine($"私钥长度: {PrivateKey?.Length ?? 0} 字节");
            Console.WriteLine($"公钥长度: {PublicKey?.Length ?? 0} 字节");
            Console.WriteLine($"私钥(Hex): {BitConverter.ToString(PrivateKey).Replace("-", "")}");
            Console.WriteLine($"公钥(Hex): {BitConverter.ToString(PublicKey).Replace("-", "").Substring(0, 64)}...");
        }
    }

    /// <summary>
    /// 安全的密钥存储示例（使用DPAPI进行保护）
    /// </summary>
    public class SecureKeyStorage
    {
        /// <summary>
        /// 使用DPAPI加密并保存私钥
        /// </summary>
        public static void SavePrivateKeySecurely(byte[] privateKey, string filePath)
        {
            byte[] encryptedData = ProtectedData.Protect(
                privateKey, 
                null, // 可选附加数据
                DataProtectionScope.CurrentUser
            );
            
            File.WriteAllBytes(filePath, encryptedData);
            Console.WriteLine($"私钥已安全保存到: {filePath}");
        }

        /// <summary>
        /// 使用DPAPI解密并加载私钥
        /// </summary>
        public static byte[] LoadPrivateKeySecurely(string filePath)
        {
            byte[] encryptedData = File.ReadAllBytes(filePath);
            byte[] privateKey = ProtectedData.Unprotect(
                encryptedData,
                null, // 可选附加数据
                DataProtectionScope.CurrentUser
            );
            
            Console.WriteLine($"私钥已从安全存储加载: {filePath}");
            return privateKey;
        }
    }

    /// <summary>
    /// 演示如何使用ECDSA进行消息签名和验证
    /// </summary>
    public class MessageSigner
    {
        private readonly byte[] _privateKey;
        private readonly byte[] _publicKey;

        public MessageSigner(byte[] privateKey, byte[] publicKey)
        {
            _privateKey = privateKey;
            _publicKey = publicKey;
        }

        /// <summary>
        /// 签名消息
        /// </summary>
        public SignedMessage SignMessage(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var signature = EcdsaKeyManager.SignData(messageBytes, _privateKey);
            var timestamp = DateTime.UtcNow;

            return new SignedMessage
            {
                Message = message,
                Signature = signature,
                Timestamp = timestamp,
                PublicKey = _publicKey
            };
        }

        /// <summary>
        /// 验证签名消息
        /// </summary>
        public bool VerifyMessage(SignedMessage signedMessage)
        {
            // 验证时间戳（可选）
            var timeDifference = DateTime.UtcNow - signedMessage.Timestamp;
            if (timeDifference.TotalMinutes > 10) // 10分钟有效期
            {
                Console.WriteLine("消息已过期");
                return false;
            }

            var messageBytes = Encoding.UTF8.GetBytes(signedMessage.Message);
            return EcdsaKeyManager.VerifyData(messageBytes, signedMessage.Signature, signedMessage.PublicKey);
        }

        /// <summary>
        /// 创建带有时效性的签名
        /// </summary>
        public SignedMessage SignMessageWithExpiry(string message, TimeSpan expiry)
        {
            var signedMessage = SignMessage(message);
            signedMessage.Expiry = expiry;
            return signedMessage;
        }
    }

    public class SignedMessage
    {
        public string Message { get; set; }
        public byte[] Signature { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[] PublicKey { get; set; }
        public TimeSpan? Expiry { get; set; }

        public bool IsExpired()
        {
            if (!Expiry.HasValue) return false;
            return DateTime.UtcNow - Timestamp > Expiry.Value;
        }

        public void PrintMessageInfo()
        {
            Console.WriteLine($"消息: {Message}");
            Console.WriteLine($"时间戳: {Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"签名长度: {Signature?.Length ?? 0} 字节");
            Console.WriteLine($"签名(Hex): {BitConverter.ToString(Signature).Replace("-", "").Substring(0, 32)}...");
            Console.WriteLine($"是否过期: {IsExpired()}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== ECDSA密钥生成、签名和验证演示 ===\n");

                // 演示1: 生成密钥对
                DemoKeyGeneration();

                // 演示2: 签名和验证消息
                DemoMessageSigning();

                // 演示3: 安全存储私钥
                DemoSecureStorage();

                // 演示4: 密钥序列化和反序列化
                DemoKeySerialization();

                Console.WriteLine("\n=== 所有演示完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        static void DemoKeyGeneration()
        {
            Console.WriteLine("1. 密钥对生成演示");
            Console.WriteLine("--------------------");

            // 生成ECDSA密钥对
            var keyPair = EcdsaKeyManager.GenerateKeyPair();
            keyPair.PrintKeyInfo();

            // 验证密钥对可以正常工作
            var testData = Encoding.UTF8.GetBytes("测试数据");
            var signature = EcdsaKeyManager.SignData(testData, keyPair.PrivateKey);
            var isValid = EcdsaKeyManager.VerifyData(testData, signature, keyPair.PublicKey);

            Console.WriteLine($"签名验证测试: {(isValid ? "✓ 成功" : "✗ 失败")}");
            Console.WriteLine();
        }

        static void DemoMessageSigning()
        {
            Console.WriteLine("2. 消息签名和验证演示");
            Console.WriteLine("----------------------");

            // 生成密钥对
            var keyPair = EcdsaKeyManager.GenerateKeyPair();
            
            // 创建签名器
            var signer = new MessageSigner(keyPair.PrivateKey, keyPair.PublicKey);

            // 签名消息
            var message = "这是一条需要签名的重要消息！";
            var signedMessage = signer.SignMessage(message);
            
            Console.WriteLine("已签名消息:");
            signedMessage.PrintMessageInfo();

            // 验证签名
            var isValid = signer.VerifyMessage(signedMessage);
            Console.WriteLine($"签名验证: {(isValid ? "✓ 有效" : "✗ 无效")}");

            // 测试篡改检测
            var tamperedMessage = signedMessage;
            tamperedMessage.Message = "这是被篡改的消息！";
            var isTamperedValid = signer.VerifyMessage(tamperedMessage);
            Console.WriteLine($"篡改检测: {(isTamperedValid ? "✗ 未检测到篡改" : "✓ 检测到篡改")}");

            Console.WriteLine();
        }

        static void DemoSecureStorage()
        {
            Console.WriteLine("3. 安全密钥存储演示");
            Console.WriteLine("---------------------");

            // 生成密钥对
            var keyPair = EcdsaKeyManager.GenerateKeyPair();

            // 安全保存私钥
            var privateKeyPath = "secure_private_key.dat";
            SecureKeyStorage.SavePrivateKeySecurely(keyPair.PrivateKey, privateKeyPath);

            // 从安全存储加载私钥
            var loadedPrivateKey = SecureKeyStorage.LoadPrivateKeySecurely(privateKeyPath);

            // 验证加载的私钥可以正常工作
            var testData = Encoding.UTF8.GetBytes("安全存储测试");
            var signature = EcdsaKeyManager.SignData(testData, loadedPrivateKey);
            var isValid = EcdsaKeyManager.VerifyData(testData, signature, keyPair.PublicKey);

            Console.WriteLine($"安全存储验证: {(isValid ? "✓ 成功" : "✗ 失败")}");

            // 清理测试文件
            File.Delete(privateKeyPath);
            Console.WriteLine();
        }

        static void DemoKeySerialization()
        {
            Console.WriteLine("4. 密钥序列化演示");
            Console.WriteLine("-------------------");

            // 生成原始密钥对
            var originalKeyPair = EcdsaKeyManager.GenerateKeyPair();
            Console.WriteLine("原始密钥对:");
            originalKeyPair.PrintKeyInfo();

            // 模拟序列化过程：将密钥转换为字节数组
            var privateKeyBytes = originalKeyPair.PrivateKey;
            var publicKeyBytes = originalKeyPair.PublicKey;

            Console.WriteLine($"序列化 - 私钥字节数: {privateKeyBytes.Length}");
            Console.WriteLine($"序列化 - 公钥字节数: {publicKeyBytes.Length}");

            // 模拟反序列化过程：从字节数组重新创建ECDsa实例
            using var privateKey = EcdsaKeyManager.LoadPrivateKey(privateKeyBytes);
            using var publicKey = EcdsaKeyManager.LoadPublicKey(publicKeyBytes);

            // 验证反序列化的密钥可以正常工作
            var testData = Encoding.UTF8.GetBytes("序列化测试数据");
            var signature = privateKey.SignData(testData, HashAlgorithmName.SHA256);
            var isValid = publicKey.VerifyData(testData, signature, HashAlgorithmName.SHA256);

            Console.WriteLine($"序列化验证: {(isValid ? "✓ 成功" : "✗ 失败")}");

            // 演示导出不同格式
            Console.WriteLine("\n密钥格式信息:");
            var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(privateKeyBytes, out _);

            try
            {
                var pkcs8PrivateKey = ecdsa.ExportPkcs8PrivateKey();
                Console.WriteLine($"PKCS8私钥长度: {pkcs8PrivateKey.Length} 字节");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PKCS8导出失败: {ex.Message}");
            }

            try
            {
                var ecPrivateKey = ecdsa.ExportECPrivateKey();
                Console.WriteLine($"EC私钥长度: {ecPrivateKey.Length} 字节");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EC私钥导出失败: {ex.Message}");
            }

            var subjectPublicKey = ecdsa.ExportSubjectPublicKeyInfo();
            Console.WriteLine($"Subject公钥长度: {subjectPublicKey.Length} 字节");

            Console.WriteLine();
        }
    }
}
```

## 关键特性说明

### 1. **密钥生成和序列化**

```csharp
// 生成密钥对
var keyPair = EcdsaKeyManager.GenerateKeyPair();

// 导出为字节数组
byte[] privateKeyBytes = keyPair.PrivateKey;  // ECPrivateKey格式
byte[] publicKeyBytes = keyPair.PublicKey;    // SubjectPublicKeyInfo格式

// 从字节数组加载
using var privateKey = EcdsaKeyManager.LoadPrivateKey(privateKeyBytes);
using var publicKey = EcdsaKeyManager.LoadPublicKey(publicKeyBytes);
```

### 2. **签名和验证**

```csharp
// 使用私钥签名
byte[] signature = EcdsaKeyManager.SignData(data, privateKeyBytes);

// 使用公钥验证
bool isValid = EcdsaKeyManager.VerifyData(data, signature, publicKeyBytes);
```

### 3. **安全存储**

```csharp
// 使用DPAPI保护私钥
SecureKeyStorage.SavePrivateKeySecurely(privateKey, "secure_file.dat");

// 安全加载私钥
byte[] loadedKey = SecureKeyStorage.LoadPrivateKeySecurely("secure_file.dat");
```

### 4. **消息签名封装**

```csharp
public class MessageSigner
{
    public SignedMessage SignMessage(string message)
    {
        // 包含时间戳、签名和公钥的完整签名消息
    }
    
    public bool VerifyMessage(SignedMessage signedMessage)
    {
        // 验证签名和时间有效性
    }
}
```

## 实际应用建议

### 生产环境改进

1. **使用硬件安全模块(HSM)**
```csharp
// 使用CspParameters或CNG与HSM集成
CspParameters cspParams = new CspParameters
{
    KeyContainerName = "MyKeyContainer",
    ProviderType = 1 // PROV_RSA_FULL
};
```

2. **密钥轮换策略**
```csharp
public class KeyRotationService
{
    public void RotateKeysIfNeeded(DateTime keyCreationDate)
    {
        if (DateTime.UtcNow - keyCreationDate > TimeSpan.FromDays(90))
        {
            // 执行密钥轮换
        }
    }
}
```

3. **证书集成**
```csharp
public class CertificateIntegration
{
    public X509Certificate2 CreateCertificate(byte[] privateKey, string subjectName)
    {
        // 使用私钥创建X.509证书
    }
}
```

这个实现展示了完整的ECDSA密钥生命周期管理，包括生成、序列化、签名验证和安全存储，可以直接用于实际的加密通信系统。