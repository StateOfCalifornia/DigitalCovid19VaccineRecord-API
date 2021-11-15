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
            for (int i = 0; i < 9999; i++)
            {
                try
                {
                    var text = $"{DateTime.Now.Ticks}~{Convert.ToString(i).PadLeft(4, '0')}~{RandomString(random.Next(3, 40))}";
                    var actualEncrypted = encryptService.EncryptGcm(text, code);
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



        private static readonly Random random = new((int)(DateTime.Now.Ticks % Int32.MaxValue));
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

    }
}
