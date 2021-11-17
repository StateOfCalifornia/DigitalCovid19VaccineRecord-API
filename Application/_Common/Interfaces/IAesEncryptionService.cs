
namespace Application.Common.Interfaces
{
    public interface IAesEncryptionService
    {
 
        public string EncryptGcm(string text, string keyString);
        public string DecryptGcm(string cipherText, string keyString);


        public string Hash(string text);
    }
}
