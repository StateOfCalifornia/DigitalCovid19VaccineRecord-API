using Application.Common.Models;
using Application.Options;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace InfrastructureTests
{
    public class JwtSignTests
    {
        private readonly Base64UrlUtility b64Utility;
        private readonly ITestOutputHelper output;
        private readonly JwtSign jwtSign;
        private readonly KeySettings _keySettings;
        private readonly string certificateString = ConfigUtilities.GetConfigValue("KeySettings:Certificate");
        private readonly string privateKeyString = ConfigUtilities.GetConfigValue("KeySettings:PrivateKey");
        private readonly string googlePrivateKey = ConfigUtilities.GetConfigValue("KeySettings:GooglePrivateKey");
        private readonly string googleCertificate = ConfigUtilities.GetConfigValue("KeySettings:GoogleCertificate");

        public JwtSignTests(ITestOutputHelper output)
        {
            b64Utility = new Base64UrlUtility();
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<JwtSign>();
            _keySettings = new KeySettings
            {
                Certificate = certificateString,
                PrivateKey = privateKeyString,
                Issuer = "https://fake",
                GooglePrivateKey = googlePrivateKey,
                GoogleCertificate = googleCertificate
            };
            jwtSign = new JwtSign(b64Utility, _keySettings);
            this.output = output;
        }
 
        [Fact]
        public void ConvertToUnixTimeTest()
        {
            var unixTime = jwtSign.ToUnixTimestamp(Convert.ToDateTime("2033-05-17 20:33:20"));//GMT
            Assert.Equal(2000000000, unixTime);
        }

        [Fact]
        public void TestSignAndVerify()
        {

            var data = new byte[] { 6, 5, 4, 3, 2 };

            var keyPair = GetKeyPair();
            bool ver = true; ;
            for (int i = 0; i < 100; i++)
            {
                var (_, r, s) = SignData(data, keyPair.Private);
                ver = Verify(r,s, data, keyPair.Public);
                output.WriteLine($"ver={ver}");
            }
            Assert.True(ver);

        }

        public void TestRsaSignAndVerify()
        {

            var data = new byte[] { 6, 5, 4, 3, 2 };

            var jwt = jwtSign.SignWithRsaKey(data);
            var ver = RsaVerify(data,Encoding.UTF8.GetBytes(jwt.Split('.')[2]));
            Assert.StartsWith("ey", jwt);
            Assert.True(ver);
        }


        public (byte[], BigInteger, BigInteger) SignData(byte[] data, AsymmetricKeyParameter privateKey)
        {
            var signer = new ECDsaSigner();

            signer.Init(true, privateKey);
            var bis = signer.GenerateSignature(data);
            var r = bis[0];
            var s = bis[1];
            output.WriteLine($"r={r}, s={s}");
             var rBytes = r.ToByteArrayUnsigned();
            var sBytes = s.ToByteArrayUnsigned();
            output.WriteLine($"rlength={rBytes.Length}, slength={sBytes.Length}");
            var combinedBytes = new Byte[rBytes.Length + sBytes.Length];
            Buffer.BlockCopy(rBytes, 0, combinedBytes, 0, rBytes.Length);
            Buffer.BlockCopy(sBytes, 0, combinedBytes, rBytes.Length, sBytes.Length);
            return (combinedBytes,r,s);

        }
        public static bool Verify(BigInteger r, BigInteger s,  byte[] data, AsymmetricKeyParameter publicKey)
        {
            var signer = new ECDsaSigner();

            signer.Init(false, publicKey);
            var bis = signer.VerifySignature(data, r, s);
            return bis;
        }

        public bool RsaVerify(byte[] signature, byte[] data)
        {
            var ipCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(googleCertificate);
            var rsa = (RSACryptoServiceProvider)ipCert.PublicKey.Key;
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        [Fact]
        public void GenerateJwksJson()
        {
            using var textReader = new StringReader(certificateString);
           
            Org.BouncyCastle.X509.X509Certificate bcCertificate = (X509Certificate)new PemReader(textReader).ReadObject();
            var publicKeyBytes = bcCertificate.CertificateStructure.SubjectPublicKeyInfo.PublicKeyData.GetBytes();
            var length = (publicKeyBytes.Length - 1) / 2;
            byte[] x = new byte[length];
            byte[] y = new byte[length];
            for (var inx = 0; inx < length; inx++)
            {
                x[inx] = publicKeyBytes[inx + 1];
                y[inx] = publicKeyBytes[inx + 1 + length];
            }
            var xCoord = b64Utility.Encode(x);
            var yCoord = b64Utility.Encode(y);
            var crv = "P-256";
            var kty = "EC";
            var alg = "ES256";
            var use = "sig";
            var thumbprint = jwtSign.GetThumbprint(certificateString);
            var key = new Key()
            {
                Kid = jwtSign.GetKid(thumbprint),
                X = xCoord,
                Y = yCoord,
                Alg = alg,
                Crv = crv,
                Kty = kty,
                Use = use
            };
            output.WriteLine("HHHHHHH--  " + BitConverter.ToString(x));
            output.WriteLine("TTTTTTT--  " + BitConverter.ToString(y));
            var jwksJson = new Jwks()
            {
                Keys = new List<Key> { key }
            };

            var jsonString = JsonConvert.SerializeObject(jwksJson, Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            output.WriteLine(jsonString);
        }

  
        // get key pair from two local files
        private AsymmetricCipherKeyPair GetKeyPair()
        {
            AsymmetricKeyParameter privateKey, publicKey;

            using (var textReader = new StringReader(privateKeyString))
            {
                // Only a private key
                var pseudoKeyPair = (AsymmetricCipherKeyPair)new PemReader(textReader).ReadObject();
                privateKey = pseudoKeyPair.Private;
            }

            using (var textReader = new StringReader(certificateString))
            {
                // Only a private key
                Org.BouncyCastle.X509.X509Certificate bcCertificate = (X509Certificate)new PemReader(textReader).ReadObject();
                publicKey = bcCertificate.GetPublicKey();
            }

            return new AsymmetricCipherKeyPair(publicKey, privateKey);

        }
    }
}
