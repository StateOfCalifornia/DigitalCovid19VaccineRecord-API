using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Options;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using Application.VaccineCredential.Queries.GetVaccineStatus;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VaccineCredential.Api.Controllers
{
    [Produces("application/json")]
    [Route("")]
    public class TestController : BaseController
    {
        private readonly AppSettings _appSettings;
        private readonly IAesEncryptionService _aesEncryptionService;
        public TestController(AppSettings appSettings, IAesEncryptionService aesEncryptionService)
        {
            _appSettings = appSettings;
            _aesEncryptionService = aesEncryptionService;
        }
        
        
        [HttpPost("testing", Name = nameof(Testing))]
        private async Task<ActionResult> Testing([FromBody] GetVaccineCredentialQuery request)
        {
            //Send command off and return the updated employee
            _appSettings.MaxQrTries = "100000";
            var testCsv = System.IO.File.ReadAllLines($"c:\\temp\\test.csv");
            var pin = "1122";
            foreach (var line in testCsv)
            {
                var id = $"{DateTime.Now.Ticks}~{pin}~{line.Trim()}";
                request = new GetVaccineCredentialQuery
                {
                    Id = _aesEncryptionService.Encrypt(id, _appSettings.CodeSecret),
                    Pin = pin,
                    WalletCode = "G"
                };
                var vm = await Mediator.Send(request);
                if (vm.VaccineCredentialViewModel != null)
                {
                    var png = Convert.FromBase64String(vm.VaccineCredentialViewModel.FileContentQr);
                    System.IO.File.WriteAllBytes($"c:\\temp\\qrcodes\\{line.Trim()}.png", png);
                    System.IO.File.WriteAllText($"c:\\temp\\models\\{line.Trim()}.json", Newtonsoft.Json.JsonConvert.SerializeObject(vm.VaccineCredentialViewModel));
                }
                else
                {
                    System.IO.File.WriteAllText($"c:\\temp\\errors\\{line.Trim()}.txt", Newtonsoft.Json.JsonConvert.SerializeObject(vm));
                }

            }
            return Ok();
        }

     
    }
}