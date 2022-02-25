using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Application.Options;
using System.Text;
using System.Security.Cryptography;
using Application.Common.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using Application.Common;
using System.Web;
using System.Security;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public class GetVaccineCredentialQueryHandler : IRequestHandler<GetVaccineCredentialQuery, VaccineCredentialModel>
    {
        private readonly ISnowFlakeService _snowFlakeService;
        private readonly ILogger<GetVaccineCredentialQueryHandler> _logger;
        private readonly IJwtSign _jwtSign;
        private readonly IJwtChunk _jwtChunk;
        private readonly ICompact _compactor;
        private readonly ICredentialCreator _credCreator;
        private readonly IQrApiService _qrApiService;
        private readonly IAesEncryptionService _aesEncryptionService;
        private readonly AppSettings _appSettings;
        private readonly IRateLimitService _rateLimitService;
        private readonly int NUMBER_OF_DOSES = 5;


        public GetVaccineCredentialQueryHandler(IRateLimitService rateLimitService, AppSettings appSettings, IAesEncryptionService aesEncryptionService, IQrApiService qrApiService, ICompact compactor, ICredentialCreator credCreator, IJwtSign jwtSign, IJwtChunk jwtChunk, ISnowFlakeService snowFlakeService, ILogger<GetVaccineCredentialQueryHandler> logger)
        {
            _snowFlakeService = snowFlakeService;
            _logger = logger;
            _jwtSign = jwtSign;
            _jwtChunk = jwtChunk;
            _credCreator = credCreator;
            _compactor = compactor;
            _qrApiService = qrApiService;
            _aesEncryptionService = aesEncryptionService;
            _appSettings = appSettings;
            _rateLimitService = rateLimitService;

        }

        public async Task<VaccineCredentialModel> Handle(GetVaccineCredentialQuery request,
            CancellationToken cancellationToken)
        {
            var message = $"The id is {Utils.Sanitize(request.Id)}";
            _logger.LogInformation(message);
            var rateLimit = await CallRegulate(request.Id);
            var vaccineCredentialModel = new VaccineCredentialModel
            {
                RateLimit = rateLimit,
                VaccineCredentialViewModel = null
            };
            if (rateLimit.Remaining < 0)
            {
                return vaccineCredentialModel;
            }
            var id = "";
            string pin;
            DateTime dateCreated;
            // 0.  Decrypt id with secretkey to get date~id
            
            try
            {
                var decrypted = "";
                try
                {
                    decrypted = _aesEncryptionService.DecryptGcm(request.Id, _appSettings.CodeSecret);
                }
                catch (Exception)
                {
                    //If there is a CodeSecretOld configured and the current codesecret failed, try with the old.
                    //There should only be CodeSecretOld set on rotating keys, where
                    //the CodeSecretOld is the old key value. After 24 hours the CodeSecretOld setting should be removed from the configuration.
                    if (!string.IsNullOrWhiteSpace(_appSettings.CodeSecretOld))
                    {
                        decrypted = _aesEncryptionService.DecryptGcm(request.Id, _appSettings.CodeSecretOld);
                    }
                    else
                    {
                        throw;
                    }
                }

                var dateBack = Convert.ToInt64(decrypted.Split("~")[0]);
                pin = decrypted.Split("~")[1];
                id = decrypted.Split("~")[2];

                dateCreated = new DateTime(dateBack);
            }
            catch (Exception exception)
            {
                _logger.LogInformation($"id:{id} had error: {exception.Message}.");
                return vaccineCredentialModel;
            }
            if(request.Pin != pin)
            {
                _logger.LogInformation($"id:{id} has invalid pin.");
                return vaccineCredentialModel;

            }
            if (dateCreated < DateTime.Now.Subtract(new TimeSpan(Convert.ToInt32(_appSettings.LinkExpireHours), 0, 0)))
            {
                _logger.LogInformation($"id:{id} has expired since its more than {_appSettings.LinkExpireHours} hours old.");
                return vaccineCredentialModel;
            }
            
            // Get Vaccine Credential
            Vc responseVc = await _snowFlakeService.GetVaccineCredentialSubjectAsync(id, cancellationToken);            
            _logger.LogInformation($"id:{id} being retrieved responseFoundVc={responseVc != null}.");
            
            if (responseVc != null)
            {
                try
                {
                    // 1.  Get the json credential in clean ( no spacing ) format.
                    Vci cred = _credCreator.GetCredential(responseVc);

                    //make sure cred only has at most 5 doses. (fhirBundle index starts at 0)
                    if(cred.Vc.CredentialSubject.FhirBundle.Entry.Count > NUMBER_OF_DOSES + 1)
                    {
                        var cntRemove = cred.Vc.CredentialSubject.FhirBundle.Entry.Count - (NUMBER_OF_DOSES + 1);
                        cred.Vc.CredentialSubject.FhirBundle.Entry.RemoveRange(1, cntRemove);
                    }

                    var dob = "";
                    if (DateTime.TryParse(cred.Vc.CredentialSubject.FhirBundle.Entry[0].Resource.BirthDate, out DateTime dateOfBirth))
                    {
                        dob = dateOfBirth.ToString("MM/dd/yyyy");
                    }

                    var doses = new List<Dose>();
                    for (int ix = 1; ix < cred.Vc.CredentialSubject.FhirBundle.Entry.Count; ix++)
                    {
                        var d = cred.Vc.CredentialSubject.FhirBundle.Entry[ix];
                        var doa = "";
                        if (DateTime.TryParse(d.Resource.OccurrenceDateTime, out var d2))
                        {
                            doa = d2.ToString("MM/dd/yyyy");
                        }
                        d.Resource.LotNumber = Utils.TrimString(Utils.ParseLotNumber(d.Resource.LotNumber), 20);
                        d.Resource.Performer = null; //Remove performer
                        // Provider set to null U11106
                        var dose = new Dose
                        {
                            Doa = doa,
                            LotNumber = d.Resource.LotNumber,
                            Provider = null,
                            Type = Utils.VaccineTypeNames[d.Resource.VaccineCode.Coding[0].Code]
                        };
                        doses.Add(dose);
                    }
                    var firstName = cred.Vc.CredentialSubject.FhirBundle.Entry[0].Resource.Name[0].Given[0];

                    var lastName = cred.Vc.CredentialSubject.FhirBundle.Entry[0].Resource.Name[0].Family;

                    var jsonVaccineCredential = JsonConvert.SerializeObject(cred, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    // 2. Compress it
                    var compressedJson = _compactor.Compress(jsonVaccineCredential);
                    
                    // 3. Get the signature
                    var signature = _jwtSign.Signature(compressedJson);
                    
                    var verifiableCredentials = new VerifiableCredentials
                    {
                        VerifiableCredential = new List<string> { signature }
                    };

                    var jsonVerifiableResult = JsonConvert.SerializeObject(verifiableCredentials, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    var shcs = _jwtChunk.Chunk(signature);
                    
                    var pngQr = await _qrApiService.GetQrCodeAsync(shcs[0]);
                    
                    // Wallet Content
                    string walletContent = string.Empty;

                    if (!string.IsNullOrEmpty(request.WalletCode))
                    {
                        switch (request.WalletCode.ToUpper())
                        {
                            case "A":
                                walletContent = $"{_appSettings.AppleWalletUrl}{shcs[0].Replace("shc:/","")}";
                                break;
                            case "G":
                                var googleWalletContent = _credCreator.GetGoogleCredential(cred, shcs[0]);
                                var jsonGoogleWallet = JsonConvert.SerializeObject(googleWalletContent, Formatting.None, new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });
                                var jwt = $"{_jwtSign.SignWithRsaKey(Encoding.UTF8.GetBytes(jsonGoogleWallet))}";
                                if (_appSettings.GoogleWalletUrl.Contains("<jwt>"))
                                {
                                    //new way with intent://
                                    walletContent = $"{_appSettings.GoogleWalletUrl}".Replace("<jwt>", jwt);
                                }
                                else
                                {
                                    //old way with just https://
                                    walletContent = $"{_appSettings.GoogleWalletUrl}{jwt}";
                                }
                                break;
                            default:
                                break;
                        } 
                    }

                    vaccineCredentialModel.VaccineCredentialViewModel = new VaccineCredentialViewModel
                    {
                        Doses = doses,
                        DOB = dob,
                        FirstName = firstName,
                        //MiddleName = middleName,
                        LastName = lastName,
                        FileNameSmartCard = "CACovid19HealthCard.smart-health-card",
                        FileContentSmartCard = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonVerifiableResult)),
                        MimeTypeSmartCard = "application/smart-health-card",
                        FileNameQr = "QR_Code.png",
                        FileContentQr = Convert.ToBase64String(pngQr),
                        MimeTypeQr = "image/png",
                        WalletContent = walletContent
                    };

                    _logger.LogInformation($"RECEIVED-QR returning ... for  {firstName.Substring(0, 1)}.{lastName.Substring(0, 1)}.");
                    return vaccineCredentialModel;
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"CORRUPTDATA-QR returning ... for  id:{id}  error:{e.Message}  {e.StackTrace}");
                    vaccineCredentialModel.CorruptData = true ;
                }
            }
            _logger.LogInformation($"MISSING-QR returning ... for  id:{id}");
            return vaccineCredentialModel;
        }

        private async Task<RateLimit> CallRegulate(string id)
        {

            var rateLimit = await _rateLimitService.RateLimitAsync(
                id, 
                Convert.ToInt32(_appSettings.MaxQrTries),
                TimeSpan.FromSeconds(Convert.ToInt32(_appSettings.MaxQrSeconds)));

            return rateLimit;
        }
    }
}
