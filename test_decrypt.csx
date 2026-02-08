using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

string key = "3b580231127936e3761840850eb85a5620ba53dc48a0590464123dd8881bd13d";
byte[] salt = Encoding.UTF8.GetBytes("AceJobAgencySalt");
string[] nrics = { "XBo8qnNo0Zo85lHk4xtuZQ==", "BTnn6jTn/30xMCJLVhAuLA==", "xNbZOdo0QC5ub2pcjIkCCQ==" };
string[] emails = { "aaoidd@gmail.com", "asdadq@gmail.com", "corbin15dec@gmail.com" };

byte[] DeriveKey(string password) {
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
    return pbkdf2.GetBytes(32);
}

for (int i = 0; i < nrics.Length; i++) {
    try {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(key);
        aes.IV = new byte[16];
        var buffer = Convert.FromBase64String(nrics[i]);
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        Console.WriteLine($"{emails[i]}: {sr.ReadToEnd()}");
    } catch (Exception ex) {
        Console.WriteLine($"{emails[i]}: FAILED - {ex.GetType().Name}: {ex.Message}");
    }
}
