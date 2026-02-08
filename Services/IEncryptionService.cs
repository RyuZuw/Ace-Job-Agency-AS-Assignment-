namespace AceJobAgency.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        /// <summary>
        /// Computes a deterministic HMAC-SHA256 hash for equality comparisons (e.g. duplicate NRIC check).
        /// </summary>
        string ComputeHash(string plainText);
    }
}
