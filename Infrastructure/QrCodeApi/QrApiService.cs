using System;
using System.Net.Http;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;


namespace Infrastructure.QrApi
{
    public class QrApiService : IQrApiService
    {
        private readonly ILogger<QrApiService> _logger;
        private readonly AppSettings _appSettings;

        #region Constructor

        public QrApiService(ILogger<QrApiService> logger, AppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }


        #endregion

        #region ISnowFlakeService Implementation

        public async  Task<byte[]> GetQrCodeAsync(string shc)
        {
            var content = new StringContent(shc);

            HttpClient client = new HttpClient();
            var response = await client.PostAsync(_appSettings.QrCodeApi, content);

            var responseString = await response.Content.ReadAsStringAsync();

            var base64Part = responseString.Replace("data:image/png;base64,", "");

            var pngContent = Convert.FromBase64String(base64Part);
            return pngContent;
        }
        #endregion
    }
}
