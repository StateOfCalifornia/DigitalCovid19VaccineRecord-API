using System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Org.BouncyCastle.Asn1.Sec;

namespace InfrastructureTests
{
    public class JwtSign2
    {

        [Fact]
        public void DoIt() => Mainx();

        private static void Mainx()
        {
            string privateKey = ConfigUtilities.GetConfigValue("Hex:PrivateKey");
            string publicKey = ConfigUtilities.GetConfigValue("Hex:PublicKey");

            var privateECDsa = LoadPrivateKey(FromHexString(privateKey));
            var publicECDsa = LoadPublicKey(FromHexString(publicKey));

            var jwt = CreateSignedJwt(privateECDsa);
            var isValid = VerifySignedJwt(publicECDsa, jwt);

            Console.WriteLine(isValid ? "Valid!" : "Not Valid...");
        }

        // https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        private static byte[] FromHexString(string hex)
        {
            var numberChars = hex.Length;
            var hexAsBytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
            {
                hexAsBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return hexAsBytes;
        }

        private static ECDsa LoadPrivateKey(byte[] key)
        {
            var privKeyInt = new Org.BouncyCastle.Math.BigInteger(+1, key);
            var parameters = SecNamedCurves.GetByName("secp256r1");
            var ecPoint = parameters.G.Multiply(privKeyInt);
            var privKeyX = ecPoint.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned();
            var privKeyY = ecPoint.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned();

            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = privKeyInt.ToByteArrayUnsigned(),
                Q = new ECPoint
                {
                    X = privKeyX,
                    Y = privKeyY
                }
            });
        }

        private static ECDsa LoadPublicKey(byte[] key)
        {
            var pubKeyX = key.Skip(1).Take(32).ToArray();
            var pubKeyY = key.Skip(33).ToArray();

            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = pubKeyX,
                    Y = pubKeyY
                }
            });
        }

        private static string CreateSignedJwt(ECDsa eCDsa)
        {
            var now = DateTime.UtcNow;
            var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.CreateJwtSecurityToken(
                issuer: "me",
                audience: "you",
                subject: null,
                notBefore: now,
                expires: now.AddMinutes(30),
                issuedAt: now,
                signingCredentials: new SigningCredentials(new ECDsaSecurityKey(eCDsa), SecurityAlgorithms.EcdsaSha256));

            jwtToken.Header.Add("kid", "aaaaa");
            var token = tokenHandler.WriteToken(jwtToken);
            return token;
        }

        private static bool VerifySignedJwt(ECDsa eCDsa, string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = "me",
                ValidAudience = "you",
                IssuerSigningKey = new ECDsaSecurityKey(eCDsa)
            }, out _);

            return claimsPrincipal.Identity.IsAuthenticated;
        }
    }
}
