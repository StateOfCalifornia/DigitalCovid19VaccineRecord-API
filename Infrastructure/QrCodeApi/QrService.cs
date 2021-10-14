using System.Collections.Generic;
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
    public class QrService : IQrApiService
    {
        private readonly ILogger<QrApiService> _logger;
        private readonly AppSettings _appSettings;

        #region Constructor

        public QrService(ILogger<QrApiService> logger, AppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }


        #endregion

        #region ISnowFlakeService Implementation

        public async Task<byte[]> GetQrCodeAsync(string shc)
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
            return await Task.FromResult<byte[]>(ms.ToArray());
        }
        #endregion
    }
}
