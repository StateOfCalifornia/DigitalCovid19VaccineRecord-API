
namespace Application.Common.Interfaces
{
    public interface IAesEncryptionService
    {
        string Encrypt(string data, string key);

        string Decrypt(string data, string secretKey);
        public string Hash(string text);
    }
}
