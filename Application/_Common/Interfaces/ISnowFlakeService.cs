using System.Threading;
using System.Threading.Tasks;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using Application.VaccineCredential.Queries.GetVaccineStatus;
using Snowflake.Data.Client;

namespace Application.Common.Interfaces
{
    public interface ISnowFlakeService
    {
        Task<Vc> GetVaccineCredentialSubjectAsync(string id, CancellationToken cancellationToken);
        Task<string> GetVaccineCredentialStatusAsync(GetVaccineCredentialStatusQuery request, CancellationToken cancellationToken);
        Task<string> GetVaccineCredentialStatusAsync(SnowflakeDbConnection conn, GetVaccineCredentialStatusQuery request, CancellationToken cancellationToken);
    }
}
