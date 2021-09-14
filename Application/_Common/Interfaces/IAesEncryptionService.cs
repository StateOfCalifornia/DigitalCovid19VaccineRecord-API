
namespace Application.Common.Interfaces
{
    public interface IAesEncryptionService
    {
        string Encrypt(string data, string key);

        string Decrypt(string data, string secretKey);

        public string EncryptGcm(string text, string keyString);
        public string DecryptGcm(string cipherText, string keyString);


        public string Hash(string text);
    }
}
