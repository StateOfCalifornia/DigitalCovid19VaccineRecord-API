using Application.Common.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure
{
    public class AesEncryptionService : IAesEncryptionService
    {
        private readonly IBase64UrlUtility _b64Url;
        private readonly string PreString = "PreCheck";
        private readonly string PostString = "PostCheck";
        public AesEncryptionService(IBase64UrlUtility b64Url)
        {
            _b64Url = b64Url;
        }

        public string Encrypt(string text, string keyString)
        {

            var key = Encoding.UTF8.GetBytes(keyString);

            using var aesAlg = Aes.Create("AesManaged");
            using var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV);
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(PreString + text + PostString);
            }

            var iv = aesAlg.IV;

            var decryptedContent = msEncrypt.ToArray();

            var result = new byte[iv.Length + decryptedContent.Length];

            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

            return _b64Url.Encode(result);
        }



        public string Decrypt(string cipherText, string keyString)
        {
            var fullCipher = _b64Url.Decode(cipherText);

            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, fullCipher.Length - iv.Length);

            var key = Encoding.UTF8.GetBytes(keyString);

            using var aesAlg = Aes.Create("AesManaged");
            using var decryptor = aesAlg.CreateDecryptor(key, iv);
            string result;
            using (var msDecrypt = new MemoryStream(cipher))
            {
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                result = srDecrypt.ReadToEnd();
            }
            //strip off first and last 3 which should be AAA...AAA
            if (!result.StartsWith(PreString))
            {
                throw new ArgumentException("Bad Encryption Value");
            }
            if (!result.EndsWith(PostString))
            {
                throw new ArgumentException("Bad Encryption Value");
            }
            result = result[this.PreString.Length..];
            result = result.Substring(0, result.Length - PostString.Length);
            return result;
        }

        public string Hash(string text)
        {
            var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);

            // Choose correct encoding based on your usecase
            byte[] input = Encoding.ASCII.GetBytes(text);

            hashAlgorithm.BlockUpdate(input, 0, input.Length);

            byte[] result = new byte[64]; // 512 / 8 = 64
            hashAlgorithm.DoFinal(result, 0);

            string hashString = BitConverter.ToString(result);
            hashString = hashString.Replace("-", "").ToLowerInvariant();
            return hashString;

        }
        public static string StringSha512Hash(string text) =>
string.IsNullOrEmpty(text) ? string.Empty : BitConverter.ToString(new SHA512Managed().ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty);

    }
}
