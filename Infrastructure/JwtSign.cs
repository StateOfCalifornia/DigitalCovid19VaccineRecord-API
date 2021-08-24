using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Models;
using Application.Common.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;

namespace Infrastructure
{
    public class JwtSign : IJwtSign
    {
        private readonly IBase64UrlUtility _base64UrlUtility;
        private readonly KeySettings _keySettings;

        public JwtSign(IBase64UrlUtility base64UrlUtility, KeySettings keySettings)
        {
            _base64UrlUtility = base64UrlUtility;
            _keySettings = keySettings;
        }

        public string SignWithRsaKey(byte[] payload)
        {
            var segments = new List<string>();
            var header = new { alg = "RS256", typ = "JWT" };

            byte[] headerBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(header, Formatting.None));
          
            segments.Add(_base64UrlUtility.Encode(headerBytes));
            segments.Add(_base64UrlUtility.Encode(payload));

            string stringToSign = string.Join(".", segments.ToArray());

            byte[] bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

            byte[] keyBytes = Convert.FromBase64String(_keySettings.GooglePrivateKey);

            var privKeyObj = Asn1Object.FromByteArray(keyBytes);
            var privStruct = RsaPrivateKeyStructure.GetInstance((Asn1Sequence)privKeyObj);

            ISigner sig = SignerUtilities.GetSigner("SHA256withRSA");

            sig.Init(true, new RsaKeyParameters(true, privStruct.Modulus, privStruct.PrivateExponent));

            sig.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
            byte[] signature = sig.GenerateSignature();

            segments.Add(_base64UrlUtility.Encode(signature));
            return string.Join(".", segments.ToArray());
        }
       

        public string Signature(byte[] payload)
        {
            var thumb = GetThumbprint(_keySettings.Certificate);
            var kid = GetKid(thumb);

            var header = @$"{{""zip"":""DEF"",""alg"":""ES256"",""kid"":""{kid}""}}".Replace(" ","");
            var header64 = _base64UrlUtility.Encode(Encoding.ASCII.GetBytes(header));
            var payload64 = _base64UrlUtility.Encode(payload);
            using var textReader = new StringReader(_keySettings.PrivateKey);
           
            var pseudoKeyPair = (AsymmetricCipherKeyPair)new PemReader(textReader).ReadObject();
            var privateKey = pseudoKeyPair.Private;

            var headerBytes = Encoding.UTF8.GetBytes(header64);
            var payloadBytes = Encoding.UTF8.GetBytes(payload64);
            var periodBytes = Encoding.UTF8.GetBytes(".");
            var combinedBytes = new Byte[headerBytes.Length + payloadBytes.Length + periodBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, combinedBytes, 0, headerBytes.Length);
            Buffer.BlockCopy(periodBytes, 0, combinedBytes, headerBytes.Length, periodBytes.Length);
            Buffer.BlockCopy(payloadBytes, 0, combinedBytes, headerBytes.Length + periodBytes.Length, payloadBytes.Length);

            var macced = SHA256.Create().ComputeHash(combinedBytes);
            var signedData = SignData(macced, privateKey);
            var signedData64 = _base64UrlUtility.Encode(signedData);

            return header64 + "." + payload64 + "." + signedData64;
        }
        public static byte [] SignData(byte[] data, AsymmetricKeyParameter privateKey)
        {
            byte[] combinedBytes = new byte[0];
            int tries = 0;
            int maxTries = 100;
            var signer = new ECDsaSigner();

            signer.Init(true, privateKey);
            // Make sure signature is 64 bytes.
            // about 0.7% of the time it is 63 bytes.
            while (combinedBytes.Length != 64 && tries++ < maxTries)
            {
                var bis =  signer.GenerateSignature(data);
                var r = bis[0];
                var s = bis[1];
                var rBytes = r.ToByteArrayUnsigned();
                var sBytes = s.ToByteArrayUnsigned();
                combinedBytes = new Byte[rBytes.Length + sBytes.Length];
                Buffer.BlockCopy(rBytes, 0, combinedBytes, 0, rBytes.Length);
                Buffer.BlockCopy(sBytes, 0, combinedBytes, rBytes.Length, sBytes.Length);
            }
            if(tries >= 100)
            {
                throw new ArgumentException("Invalid Signed Data");
            }
            return combinedBytes;

        }

        
        public long ToUnixTimestamp(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var time = date.ToUniversalTime().Subtract(epoch);
            return time.Ticks / TimeSpan.TicksPerSecond;
        }


        public Thumbprint GetThumbprint(string certificate)//(string crv, string kty, string x, string y)
        {
            using var textReader = new StringReader(certificate);
            
            var bcCertificate = (X509Certificate)new PemReader(textReader).ReadObject();
            var publicKeyBytes = bcCertificate.CertificateStructure.SubjectPublicKeyInfo.PublicKeyData.GetBytes();
            var length = (publicKeyBytes.Length - 1) / 2;
            byte[] x = new byte[length];
            byte[] y = new byte[length];
            for (var inx = 0; inx < length; inx++)
            {
                x[inx] = publicKeyBytes[inx + 1];
                y[inx] = publicKeyBytes[inx + 1 + length];
            }
            var xCoord = _base64UrlUtility.Encode(x);
            var yCoord = _base64UrlUtility.Encode(y);
            var crv = "P-256";
            var kty = "EC";

            // see rfc7638 JWK Members Used in the Thumbprint Computation
            var thumbprint = new Thumbprint
            {
                crv = crv,
                kty = kty,
                x = xCoord,
                y = yCoord
            };
            return thumbprint;
        }

        public string GetKid(Thumbprint thumbprint)
        {
            var json = JsonConvert.SerializeObject(thumbprint, Formatting.None);
            var shaCreator = SHA256.Create();
            var bytes = Encoding.ASCII.GetBytes(json);
            byte[] hash = shaCreator.ComputeHash(bytes);
            var b64 = _base64UrlUtility.Encode(hash);
            return b64;
        }
    }
}
