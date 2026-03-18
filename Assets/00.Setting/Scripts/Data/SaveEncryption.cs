using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Framework
{
    public static class SaveEncryption
    {
        private static volatile byte[] _keyBytes;

        public static void SetKey(string key)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            if (bytes.Length != 32)
                throw new ArgumentException(
                    $"Key must encode to 32 bytes (AES-256). Got {bytes.Length}. Use ASCII characters.");
            _keyBytes = bytes;
        }

        public static byte[] Encrypt(string plainText)
        {
            var key = RequireKey();

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var encryptor = aes.CreateEncryptor())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return ms.ToArray();
        }

        public static bool TryDecrypt(byte[] cipherData, out string plainText)
        {
            plainText = null;

            if (cipherData == null || cipherData.Length <= 16)
                return false;

            try
            {
                var key = RequireKey();

                var iv = new byte[16];
                Array.Copy(cipherData, 0, iv, 0, 16);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var ms = new MemoryStream(cipherData, 16, cipherData.Length - 16);
                using var decryptor = aes.CreateDecryptor();
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                plainText = sr.ReadToEnd();
                return true;
            }
            catch (CryptographicException)
            {
                Debug.LogWarning("[SaveEncryption] Decryption failed: data corrupted or key mismatch.");
                return false;
            }
            catch (InvalidOperationException)
            {
                Debug.LogWarning("[SaveEncryption] Decryption failed: key not set.");
                return false;
            }
            catch (FormatException)
            {
                Debug.LogWarning("[SaveEncryption] Decryption failed: invalid data format.");
                return false;
            }
        }

        private static byte[] RequireKey()
        {
            var key = _keyBytes;
            if (key == null)
                throw new InvalidOperationException(
                    "[SaveEncryption] Key not set. Call SaveEncryption.SetKey() during initialization.");
            return key;
        }
    }
}
