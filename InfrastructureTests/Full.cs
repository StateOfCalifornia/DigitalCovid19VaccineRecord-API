using Application.Common;
using Application.Common.Interfaces;
using Application.Options;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using Infrastructure;
using Infrastructure.QrApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace InfrastructureTests
{
    public class Full
    {  
        private static int totalCountRSAFailed = 0;
        private static int totalCounts = 0;
        private string shc0 = "shc:/567629095243206034602924374044603122295953265460346029254077280433602870286471674522280928613331456437653141590640220306450459085643550341424541364037063665417137241236380304375622046737407532323925433443326057360106452931611232742528435076726076394532075930645227370362275872247532202655562409710705372708277137253326005650540906446262305277446241053377100057320975270327272221033230440575680967664540582338673426407065367574665767210333756668242474446674340335032865533769377142105673765043716766214536575428093936605523007207040955597540355931687203124534397300383843730666632308425755602010702241322056435531385729060535042463716536445535603428393065581052703803241150697255432600363336266165073122000574573158732529360766537477377762106658272260612438566135624223206456126554042245092645112157302010710874096375393324426405282105115552403132457577046128081050343627656954631021443072335440506704327721305333052941600441397410284077106637311073080027572532546203753776433177745943122070635528341006765722546906055905771143603022560663095523332326455500652270723362680969083811743221413307293953052645535440322507687627443426332275717711532929254352586708605435565206712633236374775326666866323563710761560824680054232843705737762577651272106435396632037175111127274038376668606266033565055610290906366703505453432108691270703365110974534373312336127570017570607325667644623304611244747152623737397238074308127565687559454259215660660426265577694363365810043027074423333425416863110800655269352558343661455053057060737473543920";

        private string certificateString = ConfigUtilities.GetConfigValue("KeySettings:Certificate");
        private string privateKeyString = ConfigUtilities.GetConfigValue("KeySettings:PrivateKey");

        private string json = @"{""iss"":""https://sacodeca.blob.core.windows.net/creds"",""nbf"":637576617482895822,""vc"":{""type"":[""https://smarthealth.cards#health-card"",""https://smarthealth.cards#immunization"",""https://smarthealth.cards#covid19""],""credentialSubject"":{""fhirVersion"":""4.0.1"",""fhirBundle"":{""resourceType"":""Bundle"",""type"":""collection"",""entry"":[{""fullUrl"":""resource:0"",""resource"":{""resourceType"":""Patient"",""name"":[{""family"":""one"",""given"":[""Alpha"",""A""]}],""birthDate"":""1990-01-01""}},{""fullUrl"":""resource:1"",""resource"":{""resourceType"":""Immunization"",""status"":""completed"",""vaccineCode"":{""coding"":[{""system"":""http://hl7.org/fhir/sid/cvx"",""code"":""208""}]},""patient"":{""reference"":""resource:0""},""occurrenceDateTime"":""2021-05-01"",""lotNumber"":""ABCD1""}},{""fullUrl"":""resource:2"",""resource"":{""resourceType"":""Immunization"",""status"":""completed"",""vaccineCode"":{""coding"":[{""system"":""http://hl7.org/fhir/sid/cvx"",""code"":""208""}]},""patient"":{""reference"":""resource:0""},""occurrenceDateTime"":""2021-05-15"",""lotNumber"":""ABCD2""}}]}}}}";
        private readonly IJwtChunk _chunk;
        private readonly IJwtSign _jwt;
        private readonly ICompact _compact;
        private readonly IBase64UrlUtility _b64;
        private readonly KeySettings _keySettings;
        private readonly ITestOutputHelper _output;


        public Full(ITestOutputHelper output)
        {
            _chunk = new JwtChunk();
            _b64 = new Base64UrlUtility();
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<JwtSign>();
            _keySettings = new KeySettings();
            _keySettings.PrivateKey = privateKeyString;
            _keySettings.Certificate = certificateString;
            _keySettings.Issuer = "https://myvaccinerecord.cdph.ca.gov/creds";
            _jwt = new JwtSign(_b64, _keySettings);
            _compact = new Compact();
             _output = output;

       }

        [Fact]
        public void ShouldTransformJsonToShc()
        {

            //1.  Presume we have the same json as json0

            //2. Compress
            //var compacted = Encoding.UTF8.GetBytes(json0);// _compact.Compress(json0);
            var compacted = _compact.Compress(json);

            // 3. Sign
            var testJwt = _jwt.Signature(compacted);

            // 4. Chunk
            var split = _chunk.Chunk(testJwt);
            var shc0Combined = _chunk.Combine(new List<string> { shc0 });
  
            var splitCombined = _chunk.Combine(split);
            var verified = VerifyJwt(splitCombined);
            Assert.True(verified);
        }

        private bool VerifyJwt(string splitCombined)
        {
            var splitJwtPayload = splitCombined.Split(".")[1];
            var splitSignature = splitCombined.Split(".")[2];
            var splitSignatureBytes = _b64.Decode(splitSignature);
            var splitBytesPayload = Encoding.UTF8.GetBytes(splitJwtPayload);
            var splitBytesHeader = Encoding.UTF8.GetBytes(splitCombined.Split(".")[0]);

            var periodBytes = Encoding.UTF8.GetBytes(".");
            var combinedBytes = new Byte[splitBytesHeader.Length + splitBytesPayload.Length + periodBytes.Length];
            Buffer.BlockCopy(splitBytesHeader, 0, combinedBytes, 0, splitBytesHeader.Length);
            Buffer.BlockCopy(periodBytes, 0, combinedBytes, splitBytesHeader.Length, periodBytes.Length);
            Buffer.BlockCopy(splitBytesPayload, 0, combinedBytes, splitBytesHeader.Length + periodBytes.Length, splitBytesPayload.Length);

            var hash = SHA256.Create().ComputeHash(combinedBytes);
            using (var textReader = new StringReader(certificateString))
            {
                // Only a private key
                Org.BouncyCastle.X509.X509Certificate bcCertificate = (X509Certificate)new PemReader(textReader).ReadObject();
                var publicKey = bcCertificate.GetPublicKey();

                var verified = Verify(splitSignatureBytes, hash, publicKey);

                return verified;
            }

        }
        public bool Verify(byte[] signature, byte[] data, AsymmetricKeyParameter publicKey)
        {
            var signer = new ECDsaSigner();

            signer.Init(false, publicKey);
            var r = new BigInteger(1,signature, 0, signature.Length / 2);
            var s = new BigInteger(1,signature, signature.Length / 2, signature.Length / 2);
            var bis = signer.VerifySignature(data, r, s);
            return bis;
        }

        [Fact]
        public void ShouldGenerateCard()
        {
            var json = @"{""iss"":""https://smarthealth.cards/examples/issuer"",""nbf"":1621444043.769,""vc"":{""type"":[""https://smarthealth.cards#health-card"",""https://smarthealth.cards#immunization"",""https://smarthealth.cards#covid19""],""credentialSubject"":{""fhirVersion"":""4.0.1"",""fhirBundle"":{""resourceType"":""Bundle"",""type"":""collection"",""entry"":[{""fullUrl"":""resource:0"",""resource"":{""resourceType"":""Patient"",""name"":[{""family"":""Anyperson"",""given"":[""John"",""B.""]}],""birthDate"":""1951-01-20""}},{""fullUrl"":""resource:1"",""resource"":{""resourceType"":""Immunization"",""status"":""completed"",""vaccineCode"":{""coding"":[{""system"":""http://hl7.org/fhir/sid/cvx"",""code"":""207""}]},""patient"":{""reference"":""resource:0""},""occurrenceDateTime"":""2021-01-01"",""performer"":[{""actor"":{""display"":""ABC General Hospital""}}],""lotNumber"":""0000001""}},{""fullUrl"":""resource:2"",""resource"":{""resourceType"":""Immunization"",""status"":""completed"",""vaccineCode"":{""coding"":[{""system"":""http://hl7.org/fhir/sid/cvx"",""code"":""207""}]},""patient"":{""reference"":""resource:0""},""occurrenceDateTime"":""2021-01-29"",""performer"":[{""actor"":{""display"":""ABC General Hospital""}}],""lotNumber"":""0000007""}}]}}}}";
            json = json.Replace(" ", "");
            var compacted = _compact.Compress(json);

            // 3. Sign
            var thumb = _jwt.GetThumbprint(_keySettings.Certificate);
            var kid = _jwt.GetKid(thumb);
            var testJwt = _jwt.Signature(compacted);


            var verifiableCredentials = new VerifiableCredentials
            {
                verifiableCredential = new List<string> { testJwt }
            };

            var jsonVerifiableResult = JsonConvert.SerializeObject(verifiableCredentials, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            //System.IO.File.WriteAllText("c:\\users\\chris\\OneDrive\\Documents\\projects\\VaccineCredentials\\FullTest.smart-health-card", jsonVerifiableResult);
        }

        [Theory]
        [InlineData("ABCD 1234", "1234")]
        [InlineData("ABC2342@!@#!D234", null)]
        [InlineData("ABC D 2 34 $!!", "2")]
        [InlineData("ABCD1234", "ABCD1234")]
        [InlineData("ABCD1234-U", "ABCD1234-U")]
        [InlineData("062G20A", "062G20A")]
        [InlineData("026L20A", "026L20A")]
        [InlineData("  011L20A", "011L20A")]
        [InlineData("057G20A   ", "057G20A")]
        [InlineData("  028L20A", "028L20A")]
        [InlineData("  011J20A  ", "011J20A")]
        public void ParseLotNumberTest(string s, string lot)
        {
            var lotNumber = Utils.ParseLotNumber(s);
            Assert.Equal(lot, lotNumber);
        }

        [Theory]
        [InlineData("1241241s4124124141241241412412414124124141241241412412414124124141241241412412414", 60)]
        [InlineData("1askdfafpgadooapgjdajsjdla;djfsjsjjjjs12412414124x12414", 60)]
        [InlineData("412414124124141241241412412414", 20)]
        public void TrimStringTest(string s, int i)
        {
            var str = Utils.TrimString(s, i);
            _output.WriteLine(str);
            Assert.True(str.Length <= i);
        }

        [Theory]
        [InlineData(1, 40)]
        [InlineData(1, 50)]

        [InlineData(2, 10)]
        [InlineData(2, 40)]
        [InlineData(2, 50)]

        [InlineData(3, 40, 16)]
        [InlineData(3, 60, 20)]
        [InlineData(3, 68, 20)]

        [InlineData(4, 20)]
        [InlineData(4, 40)]

        [InlineData(4, 0)]
        [InlineData(10, 0)]
        [InlineData(15, 0, 9)]
        [InlineData(16, 0, 9)]
        [InlineData(17, 0, 9)]
        public async Task GeneralTest(int numDoses, int orgNameSize, int lotNumberSize=20)
        {
            var credCreator = new CredentialCreator(_keySettings,_jwt);
 
            var entries = new List<Entry>();
            var name = new Name
            {
                family = "Lastname "+ RandomString(42),//42
                given = new string[] { "FirstName" + RandomString(33) }.ToList()             //33
            };
            var names = new List<Name>();
            names.Add(name);
            var patientEntry = new Entry
            {
                fullUrl = "resource:0",
                resource = new Resource
                {
                    birthDate = "1955-10-01",
                    name = names,
                    resourceType = "Patient"                                        
                },
            };

            entries.Add(patientEntry);


            for(int i = 1; i <= numDoses; i++)
            {
                var dose = new Entry
                {
                    fullUrl = "resource:" + i,
                    resource = new Resource
                    {
                        lotNumber = "E" + RandomString(lotNumberSize-1),
                        resourceType = "Immunization",
                        status = "completed",
                        patient = new Patient
                        {
                            reference = "resource:0"
                        },
                        vaccineCode = new VaccineCode { coding = new List<Coding>() },
                        occurrenceDateTime = $"2021-03-0{i.ToString().Substring(0,1)}",
                        performer = orgNameSize <= 0 ? null : new List<Performer>()
                    }
                };
                if (orgNameSize > 0)
                {
                    dose.resource.performer.Add(new Performer() { actor = new Actor { display = $"ORG-{RandomString(orgNameSize - 4)}" } });
                }
                var code = new Coding { code = "208", system = "http://hl7.org/fhir/sid/cvx" };
                dose.resource.vaccineCode.coding.Add(code);
                entries.Add(dose);
            }

            var vc = new Vc
            {
                type = (new string[] { "https://smarthealth.cards#health-card", "https://smarthealth.cards#immunization", "https://smarthealth.cards#covid19" }).ToList(),
                credentialSubject = new CredentialSubject
                {
                    fhirVersion = "4.0.1",
                    fhirBundle = new FhirBundle
                    {
                        type = "collection",
                        resourceType = "Bundle",
                        entry = entries
                    }
                }
            };
            var cred = credCreator.GetCredential(vc);
            cred.nbf = _jwt.ToUnixTimestamp(DateTime.Now);
            var jsonVaccineCredential = JsonConvert.SerializeObject(cred, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            // 2. Compress it
            List<string> shcs = null;
            var signatureS = "";
            for (int i = 0; i < 1; i++)
            {
                totalCounts++;
                var compressedJson = (new Compact()).Compress(jsonVaccineCredential);
                string compressedByteHex = BitConverter.ToString(compressedJson).Replace("-", "");

                // 3. Get the signature
                signatureS = _jwt.Signature(compressedJson);
                if (_b64.Decode(signatureS.Split(".")[2]).Length != 64)
                {
                    _output.WriteLine("Hmmmm ");
                    totalCountRSAFailed++;
                }
                var v = VerifyJwt(signatureS);
                Assert.True(v);

                //var sigRSA = _jwt.SignWithRsaKey(compressedJson);
                //if (_b64.Decode(sigRSA.Split(".")[2]).Length != 256)
                //{
                //    _output.WriteLine("Hmmmm Google?");
                //    totalCountGoogleFailed++;
                //}
            }
            shcs = (new JwtChunk()).Chunk(signatureS);
            try
            {
                byte[] pngQr = Array.Empty<byte>();
                using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<QrApiService>();
                pngQr = (new QrApiService(logger, new AppSettings { QrCodeApi = "https://testdvrqrcode.azurewebsites.net/api/QRCreate" })).GetQrCode(shcs[0]);
                System.IO.File.WriteAllBytes($"c:\\temp\\qrcodes\\QRCode_{numDoses}_{orgNameSize}.png", pngQr);
            }
            catch (Exception) { }
            
            var combined = (new JwtChunk()).Combine(shcs);

            Assert.Equal(
                signatureS,
                combined
                );
        }


        public byte[] Sign1(byte[] payload, AsymmetricKeyParameter privateKey)
        {
            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, privateKey);
            signer.BlockUpdate(payload, 0, payload.Length);
            var signature = signer.GenerateSignature();
            return signature;
        }
        
        public bool VerifySignature1(byte[] signature, byte[] message, AsymmetricKeyParameter publicKey)
        {

            var verifier = SignerUtilities.GetSigner("SHA-256withECDSA");
            verifier.Init(false, publicKey);
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }

        public byte[] Sign2(byte[] payload, AsymmetricKeyParameter privateKey)
        {
            var signer = new ECDsaSigner();

            signer.Init(true, privateKey);
            var bis = signer.GenerateSignature(payload);
            var r = bis[0];
            var s = bis[1];
            var rBytes = r.ToByteArrayUnsigned();
            var sBytes = s.ToByteArrayUnsigned();
            var signature = new Byte[rBytes.Length + sBytes.Length];
            Buffer.BlockCopy(rBytes, 0, signature, 0, rBytes.Length);
            Buffer.BlockCopy(sBytes, 0, signature, rBytes.Length, sBytes.Length);
            return signature;
        }

        public bool VerifySignature2(byte[] signature, byte[] message, AsymmetricKeyParameter publicKey)
        {
            var verifier = new ECDsaSigner();
            verifier.Init(false, publicKey);
            BigInteger r = new BigInteger(1, signature, 0, signature.Length / 2);
            BigInteger s = new BigInteger(1, signature, signature.Length / 2, signature.Length /2);
            return verifier.VerifySignature(message,r,s);
        }

        private AsymmetricCipherKeyPair GetKeyPair(string privateKeyString, string certString)
        {
            AsymmetricKeyParameter privateKey, publicKey;

            using (var textReader = new StringReader(privateKeyString))
            {
                // Only a private key
                var pseudoKeyPair = (AsymmetricCipherKeyPair)new PemReader(textReader).ReadObject();
                privateKey = pseudoKeyPair.Private;
            }

            using (var textReader = new StringReader(certString))
            {
                // Only a private key
                Org.BouncyCastle.X509.X509Certificate bcCertificate = (X509Certificate)new PemReader(textReader).ReadObject();
                
                publicKey = bcCertificate.GetPublicKey();
            }

            return new AsymmetricCipherKeyPair(publicKey, privateKey);

        }

        private static Random random = new Random((int)(DateTime.Now.Ticks % 1000000000));
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";// .'-,";// ! 23~`'\":;<,>.?/\\| ";// 0123456789";
            var retstring = new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
           // if (retstring.Length > 20)
            //
           //     retstring = retstring.Substring(0, 20);
           // }
            return retstring;
        }

    }
}
