using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.EncryptLib
{
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

}
