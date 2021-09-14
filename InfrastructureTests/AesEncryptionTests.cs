using Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Xunit.Abstractions;

namespace InfrastructureTests
{
    public class AesEncryptionTests
    {

        private readonly ITestOutputHelper _logger;


          public AesEncryptionTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        [Fact]
        public void ShouldEncryptGcm()
        {
            var code = ConfigUtilities.GetConfigValue("AppSettings:CodeSecret");
            var encryptService = new AesEncryptionService(new Base64UrlUtility());
            var text = "";
            var actualEncrypted = "";

            for (int i = 0; i < 9999; i++)
            {
                try
                {
                    text = $"{DateTime.Now.Ticks}~{Convert.ToString(i).PadLeft(4, '0')}~{RandomString(random.Next(3, 40))}";
                    actualEncrypted = encryptService.EncryptGcm(text, code);
                    var length = actualEncrypted.Length;
                    var decrypted = encryptService.DecryptGcm(actualEncrypted, code);
                    _logger.WriteLine($"lengthEncrypted:{actualEncrypted.Length} text:{text} decrypted:{decrypted}");
                    Assert.Equal(text, decrypted);
                }
                catch (Exception ex)
                {
                    _logger.WriteLine($"{ex.Message}");
                }
            }

        }

        [Fact]
        public void ShouldEncryptDecrypt()
        {
            var encryptService = new AesEncryptionService(new Base64UrlUtility());
            var secret = ConfigUtilities.GetConfigValue("AppSettings:CodeSecret");
            var date = DateTime.Now.Ticks;
            var relid = "414324543~jker";
            var text = date + "~" + relid;
            var encrypted = encryptService.Encrypt(text, secret);
            var decrypted = encryptService.Decrypt(encrypted, secret);
            var dateBack = Convert.ToInt64(decrypted.Split("~")[0]);
            var relidBack = decrypted.Substring(dateBack.ToString().Length + 1);
            Assert.Equal(text, decrypted);
            Assert.Equal(date, dateBack);
            Assert.Equal(relid, relidBack);

        }

        [Fact]
        public void ShouldEncryptDecryptMany()
        {
            var code = ConfigUtilities.GetConfigValue("AppSettings:CodeSecret");
            var encryptService = new AesEncryptionService(new Base64UrlUtility());
            var text = "";
            var actualEncrypted = "";
            var usedEncrypted = "";
            for (int i = 0; i < 9999; i++)
            {
                try
                {
                    text = $"{DateTime.Now.Ticks}~{Convert.ToString(i).PadLeft(4, '0')}~{RandomString(random.Next(3, 40))}";
                    actualEncrypted = encryptService.Encrypt(text, code);
                    var length = actualEncrypted.Length;
                    usedEncrypted = actualEncrypted.Substring(0, length - (i % length));
                    var decrypted = encryptService.Decrypt("Z9Phep5GUxkzV3SIQ1m_pNfcPzFMfupsE-pzQwzFYKJNnglz1viGMx-ShAEfN3oe8cGEGZGWpbuXr7oP0D6zyYTs58WS6H4jqaBVdXos", code);
                    _logger.WriteLine($"lengthEncrypted:{actualEncrypted.Length} text:{text} decrypted:{decrypted}");
                    Assert.Equal(text, decrypted);
                }
                catch(Exception ex)
                {
                    _logger.WriteLine($"{ex.Message}");
                }
            }
        }


        private static readonly Random random = new Random((int)(DateTime.Now.Ticks % Int32.MaxValue));
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ! 23~`'\":;<,>.?/\\| ";// 0123456789";
            var retstring = new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
            if (retstring.Length > 20)
            {
                retstring = retstring.Substring(0, 20);
            }
            return retstring;
        }
        public static int RandomInteger(string chars)
        {
            var numString = new string(Enumerable.Repeat(chars, 2)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            return Int32.Parse(numString);
        }

        //[Fact]
        public void SearchCriteriaDecrypt()
        {
            var encryptService = new AesEncryptionService(new Base64UrlUtility());
            var secretKey = ConfigUtilities.GetConfigValue("AppSettings:CodeSecret");
            var keyword = "searchCriteria:";
            var text = System.IO.File.ReadAllLines("c:\\temp\\decrypt.txt");
            for (int inx = 0; inx < text.Length; inx++)
            {
                var line = text[inx];
                var cinx = line.IndexOf(keyword);
                if (cinx >= 0)
                {
                    cinx += keyword.Length;
                    var lastIndex = line.IndexOf(" ", cinx);
                    var code = lastIndex < 0 ? line[cinx..] : line.Substring(cinx, lastIndex-cinx);                    
                    text[inx] = line.Replace($"{keyword}{code}", "");
                    var searchCrit = encryptService.Decrypt(code, secretKey);
                    text[inx] = searchCrit + " " + text[inx];
                }
            }
            System.IO.File.WriteAllLines("c:\\temp\\decryptOut.txt", text);

        }
    }
}
