using System;
using System.Net.Http;
﻿using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Net.Codecrete.QrCodeGenerator;

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

        #region IQrApiService Implementation

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

        public byte[] GetQrCode(string shc)
        {
            var shcByteIndex = shc.LastIndexOf('/');
            var shcByte = shc.Substring(0,shcByteIndex+1);
            var shcNumeric = shc.Substring(shcByteIndex+1);
            
            List<QrSegment> SegmentList = new List<QrSegment>()
            {
                QrSegment.MakeBytes(Encoding.ASCII.GetBytes(shcByte)),
                QrSegment.MakeNumeric(shcNumeric)
            };
            QrCode qrCode = QrCode.EncodeSegments(SegmentList, QrCode.Ecc.Low, 22, 22);
            var bitmap = qrCode.ToBitmap(10, 4);
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray(); 
        }
        #endregion
    }
}
