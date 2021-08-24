using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using Application.VaccineCredential.Queries.GetVaccineStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VaccineCredential.Api.Controllers
{
    [Produces("application/json")]
    [Route("")]
    public class VaccineCredentialController : BaseController
    {

        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
        [HttpGet("HealthCheck")]
        public ActionResult HealthCheck([FromQuery][Bind("Pin")] string Pin)
        {
            if (ModelState.IsValid)
            {
                //Send command off and return the updated employee
                if (Pin != "654321")
                {
                    return BadRequest();
                }
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }


        [ProducesResponseType(typeof(VaccineCredentialViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status422UnprocessableEntity)]
        [HttpPost("vaccineCredential", Name = nameof(VaccineCredentialRequest))]
        public async Task<ActionResult> VaccineCredentialRequest([FromBody][Bind("Id,Pin,WalletCode")] GetVaccineCredentialQuery request)
        {
            if (ModelState.IsValid)
            {
                //Send command off and return the updated employee
                var vm = await Mediator.Send(request);
                var statusCodeResult = HandleRateLimit(vm.RateLimit);
                if (statusCodeResult != null)
                {
                    return statusCodeResult;
                }
                else if (vm.CorruptData)
                {
                    return UnprocessableEntity();
                }
                else if (vm.VaccineCredentialViewModel == null)
                {
                    return NotFound();
                }
                else
                {
                    return Ok(vm.VaccineCredentialViewModel);
                }
            }
            else
            {
                return UnprocessableEntity();
            }
        }

        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status422UnprocessableEntity)]
        [HttpPost("vaccineCredentialStatus", Name = nameof(VaccineCredentialStatusRequest))]
        public async Task<ActionResult> VaccineCredentialStatusRequest([FromBody][Bind("FirstName,LastName,DateOfBirth,PhoneNumber,EmailAddress,Pin,Language")] GetVaccineCredentialStatusQuery request)
        {
            if (ModelState.IsValid)
            {
                //Send command off and return the updated employee
                var vm = await Mediator.Send(request);
                var statusCodeResult = HandleRateLimit(vm.RateLimit);
                if (vm.InvalidPin)
                {
                    return UnprocessableEntity("Invalid Pin");
                }
                else if (statusCodeResult != null)
                {
                    return statusCodeResult;
                }
                else if (vm.ViewStatus)
                {
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return UnprocessableEntity();
            }
        }
    }
}