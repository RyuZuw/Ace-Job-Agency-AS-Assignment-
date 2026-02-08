using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace AceJobAgency.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private const string Issuer = "AceJobAgency";

        public string GenerateSecretKey()
        {
            var key = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Base32Encode(key);
        }

        public string GenerateQrCodeUri(string email, string secretKey)
        {
            return $"otpauth://totp/{Issuer}:{email}?secret={secretKey}&issuer={Issuer}";
        }

        public byte[] GenerateQrCodeImage(string qrCodeUri)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(20);
                }
            }
        }

        public bool ValidateToken(string secretKey, string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length != 6)
                return false;

            // Try current, previous, and next time windows
            for (int i = -1; i <= 1; i++)
            {
                var expectedToken = GenerateToken(secretKey, GetCurrentCounter() + i);
                if (expectedToken == token)
                    return true;
            }

            return false;
        }

        private string GenerateToken(string secretKey, long counter)
        {
            var key = Base32Decode(secretKey);
            var counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

            using (var hmac = new HMACSHA1(key))
            {
                var hash = hmac.ComputeHash(counterBytes);
                int offset = hash[hash.Length - 1] & 0x0F;
                int binary = ((hash[offset] & 0x7F) << 24) |
                            ((hash[offset + 1] & 0xFF) << 16) |
                            ((hash[offset + 2] & 0xFF) << 8) |
                            (hash[offset + 3] & 0xFF);
                int otp = binary % 1000000;
                return otp.ToString("D6");
            }
        }

        private long GetCurrentCounter()
        {
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return unixTime / 30; // 30-second time step
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            int bits = 0;
            int value = 0;

            foreach (var b in data)
            {
                value = (value << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    result.Append(alphabet[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                result.Append(alphabet[(value << (5 - bits)) & 31]);
            }

            return result.ToString();
        }

        private static byte[] Base32Decode(string encoded)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new List<byte>();
            int bits = 0;
            int value = 0;

            foreach (var c in encoded.ToUpper())
            {
                int index = alphabet.IndexOf(c);
                if (index < 0) continue;
                value = (value << 5) | index;
                bits += 5;
                if (bits >= 8)
                {
                    result.Add((byte)((value >> (bits - 8)) & 255));
                    bits -= 8;
                }
            }

            return result.ToArray();
        }
    }
}
