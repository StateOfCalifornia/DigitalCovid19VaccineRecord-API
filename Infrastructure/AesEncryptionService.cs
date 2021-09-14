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
        private const int NONCE_MAX_SIZE = 12;
        private const int TAG_MAX_SIZE = 16;
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


        public string EncryptGcm(string text, string keyString)
        {
            var key = Encoding.UTF8.GetBytes(keyString);

            using var aesAlg = new AesGcm(key);
            var textToEncrypt = Encoding.UTF8.GetBytes(text);
            var codeText = new byte[textToEncrypt.Length];

            var nonce = new byte[NONCE_MAX_SIZE];
            RandomNumberGenerator.Fill(nonce);
            var tag = new byte[TAG_MAX_SIZE];

            aesAlg.Encrypt(nonce, textToEncrypt, codeText, tag);
            var finalArray = nonce.Concat(tag).Concat(codeText).ToArray();
            return _b64Url.Encode(finalArray);
        }



        public string DecryptGcm(string cipherText, string keyString)
        {
            var fullCipher = _b64Url.Decode(cipherText);

            var key = Encoding.UTF8.GetBytes(keyString);

            var nonce = new byte [NONCE_MAX_SIZE];
            var tag = new byte[TAG_MAX_SIZE];
            var ciphertext = new byte[fullCipher.Length - nonce.Length - tag.Length];
            var plaintext = new byte[ciphertext.Length];
            Buffer.BlockCopy(fullCipher, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(fullCipher, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(fullCipher, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

            using var aesAlg = new AesGcm(key);

            aesAlg.Decrypt(nonce, ciphertext, tag, plaintext);
            var outText = Encoding.UTF8.GetString(plaintext);
            return outText;
        }



        /* based on code from stackoverflow: https://stackoverflow.com/questions/57857330/how-can-i-get-sha3-512-hash-in-c
         * from https://stackoverflow.com/users/10632330/apepenkov
         * Thanks to NineBerry(https://stackoverflow.com/users/101087/nineberry) for the idea/implementation.
         * https://stackoverflow.com/a/57857735
        */
        public string Hash(string text)
        {
            byte[] hash = new byte[64];
            var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);

            byte[] textBytes = Encoding.ASCII.GetBytes(text);

            hashAlgorithm.BlockUpdate(textBytes, 0, textBytes.Length);

            hashAlgorithm.DoFinal(hash, 0);

            string hashString = GetHashString(hash);
            return hashString;

        }

        private static string GetHashString(byte[] array)
        {
            string hashString = BitConverter.ToString(array);
            hashString = hashString.Replace("-", "").ToLowerInvariant();
            return hashString;

        }

    }
}
