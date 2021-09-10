using System.Threading;
using System.Threading.Tasks;
using Application.VaccineCredential.Queries.GetVaccineStatus;

namespace Application.Common.Interfaces
{
    public interface IQrApiService
    {
        Task<byte[]> GetQrCodeAsync(string shc);

        public byte[] GetQrCode(string shc);
    }
}
