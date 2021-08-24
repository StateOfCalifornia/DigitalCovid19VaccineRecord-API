using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Application.Options;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using Application.Common;
using Application.Common.Models;
using System.Web;

namespace Application.VaccineCredential.Queries.GetVaccineStatus
{
    public class GetVaccineCredentialStatusQueryHandler : IRequestHandler<GetVaccineCredentialStatusQuery, StatusModel>
    {
        private readonly ISnowFlakeService _snowFlakeService;
        private readonly IEmailService _emailService;
        private readonly SendGridSettings _sendGridSettings;
        private readonly ILogger<GetVaccineCredentialStatusQueryHandler> _logger;
        private readonly IMessagingService _messagingService;
        private readonly AppSettings _appSettings;
        private readonly IAesEncryptionService _aesEncryptionService;
        private readonly IQueueService _queueService;
        private readonly IRateLimitService _rateLimitService;
        
        public GetVaccineCredentialStatusQueryHandler(IRateLimitService rateLimitService, IQueueService queueService,  IAesEncryptionService aesEncryptionService, SendGridSettings sendGridSettings, IMessagingService messagingService, AppSettings appSettings, IEmailService emailService,  ISnowFlakeService snowFlakeService, ILogger<GetVaccineCredentialStatusQueryHandler> logger)
        {
            _snowFlakeService = snowFlakeService;
            _logger = logger;
            _messagingService = messagingService;
            _appSettings = appSettings;
            _emailService = emailService;
            _sendGridSettings = sendGridSettings;
            _aesEncryptionService = aesEncryptionService;
            _queueService = queueService;
            _rateLimitService = rateLimitService;
        }

        public async Task<StatusModel> Handle(GetVaccineCredentialStatusQuery request, CancellationToken cancellationToken)
        {
            var statusModel = new StatusModel();
            var rateLimiterContact = request.PhoneNumber;
            if (string.IsNullOrWhiteSpace(rateLimiterContact))
            {
                rateLimiterContact = request.EmailAddress;
            }
            var hash = _aesEncryptionService.Hash(rateLimiterContact.ToLower());
            var rateLimit = await _rateLimitService.RateLimitAsync(
                hash,
                Convert.ToInt32(_appSettings.MaxStatusTries),
                TimeSpan.FromSeconds(Convert.ToInt32(_appSettings.MaxStatusSeconds)));

            statusModel.RateLimit = rateLimit;

            if (rateLimit.Remaining < 0)
            {
                return statusModel;
            }

            var utils = new Utils(_appSettings);
            //validate pin
            var pinStatus = Utils.ValidatePin(request.Pin);
            if ( pinStatus != 0)
            {
                _logger.LogInformation("invalid pin.");
                statusModel.InvalidPin = true;
                return statusModel;
            }

            request.FirstName = request.FirstName.Trim();
            request.LastName = request.LastName.Trim();
            request.PhoneNumber = request.PhoneNumber.Trim();
            request.EmailAddress = request.EmailAddress.Trim();
            request.Language = request.Language.Trim();
             
            if (_appSettings.UseMessageQueue != "0")
            {
                statusModel.ViewStatus = await _queueService.AddMessageAsync(JsonConvert.SerializeObject(request));
                _logger.LogInformation($"ProcessedOK={statusModel.ViewStatus} added to queue for {hash}.");
            }
            else
            {
                var r = await utils.ProcessStatusRequest(_logger,_emailService, _sendGridSettings, _messagingService, _aesEncryptionService, request, _snowFlakeService, null, cancellationToken);
                statusModel.ViewStatus = r < 4;
            }
            return statusModel;
        }
    }
}
