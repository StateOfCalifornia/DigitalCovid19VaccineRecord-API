using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace VaccineCredential.Api.Services
{
    public class ClientIpHeaderTelemetry : ClientIpHeaderTelemetryInitializer
        {
            private readonly IHttpContextAccessor _httpContextAccessor;

            public ClientIpHeaderTelemetry(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
            }

            /// <summary>
            /// Implements initialization logic.
            /// </summary>
            /// <param name="platformContext">Http context.</param>
            /// <param name="requestTelemetry">Request telemetry object associated with the current request.</param>
            /// <param name="telemetry">Telemetry item to initialize.</param>
            protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

                // Mask IP Address for security reason.
                if (string.IsNullOrWhiteSpace(ipAddress)) return;

                var splitIpAddress = ipAddress.Split('.');

                var maskedIpAddress = splitIpAddress.Length > 1 ? ipAddress.Replace(splitIpAddress.Last().ToString(), "0") : ipAddress;
                _httpContextAccessor.HttpContext.Items["Masked_IP_Address"] = maskedIpAddress;
                telemetry.Context.GlobalProperties.TryAdd("IP Address", maskedIpAddress);
            }
        }
   
}
