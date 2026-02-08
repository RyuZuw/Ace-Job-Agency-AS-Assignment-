using System.Security.Cryptography;
using System.Text;

namespace AceJobAgency.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly string _key;
        private readonly byte[] _salt;

        public EncryptionService(IConfiguration configuration)
        {
            _key = configuration["Encryption:Key"] ?? throw new ArgumentNullException("Encryption key not configured");
            _salt = Encoding.UTF8.GetBytes("AceJobAgencySalt"); // 16 bytes for AES
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = DeriveKey(_key);
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        // Prefix IV to ciphertext so we can decrypt later.
                        ms.Write(aes.IV, 0, aes.IV.Length);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }

                        return "v2:" + Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Encryption failed");
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                if (cipherText.StartsWith("v2:"))
                {
                    var buffer = Convert.FromBase64String(cipherText.Substring(3));
                    if (buffer.Length <= 16)
                    {
                        throw new InvalidOperationException("Decryption failed");
                    }

                    var iv = new byte[16];
                    Array.Copy(buffer, 0, iv, 0, iv.Length);
                    var ciphertext = new byte[buffer.Length - iv.Length];
                    Array.Copy(buffer, iv.Length, ciphertext, 0, ciphertext.Length);

                    using (var aes = Aes.Create())
                    {
                        aes.Key = DeriveKey(_key);
                        aes.IV = iv;

                        using (var decryptor = aes.CreateDecryptor())
                        using (var ms = new MemoryStream(ciphertext))
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }

                // Legacy format (static IV)
                using (var legacyAes = Aes.Create())
                {
                    legacyAes.Key = DeriveKey(_key);
                    legacyAes.IV = new byte[16];

                    var legacyBuffer = Convert.FromBase64String(cipherText);
                    using (var decryptor = legacyAes.CreateDecryptor())
                    using (var ms = new MemoryStream(legacyBuffer))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Decryption failed");
            }
        }

        public string ComputeHash(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using (var hmac = new HMACSHA256(DeriveKey(_key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                return Convert.ToBase64String(hash);
            }
        }

        private byte[] DeriveKey(string password)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, _salt, 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256 bits
            }
        }
    }
}
