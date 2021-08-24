using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;
using Application.VaccineCredential.Queries.GetVaccineStatus;
using System.Data.Common;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using Newtonsoft.Json;

namespace Infrastructure.SnowFlake
{
    public class SnowFlakeService : ISnowFlakeService
    {
        private readonly ILogger<SnowFlakeService> _logger;
        private readonly SnowFlakeSettings _snowFlakeSettings;

        #region Constructor

        public SnowFlakeService(ILogger<SnowFlakeService> logger, SnowFlakeSettings snowFlakeSettings)
        {
            _logger = logger;
            _snowFlakeSettings = snowFlakeSettings;
        }

        #endregion
        class VcObject
        {
           public Vc vc { get; set; }
        }
        #region ISnowFlakeService Implementation
        public async Task<Vc> GetVaccineCredentialSubjectAsync(string id, CancellationToken cancellationToken)
        {
            Vc vaccineCredential = null;

            using (var conn = new SnowflakeDbConnection())
            {
                _logger.LogInformation($"Trying to connect to SnowFlake database.");

                conn.ConnectionString = _snowFlakeSettings.ConnectionString;
                var cmdVc = conn.CreateCommand();

                cmdVc.CommandText = _snowFlakeSettings.IdQuery;

                conn.Open();

                AddParameter(cmdVc, "1", id);

                var rdVc = await cmdVc.ExecuteReaderAsync(cancellationToken);
                if (await rdVc.ReadAsync(cancellationToken))
                {
                    var jsonString = rdVc.GetString(0);
                    var vaccineCredentialobject = JsonConvert.DeserializeObject<VcObject>(jsonString);
                    vaccineCredential = vaccineCredentialobject.vc;
                }
            }

            return vaccineCredential;
        }

        public async Task<string> GetVaccineCredentialStatusAsync(GetVaccineCredentialStatusQuery request, CancellationToken cancellationToken)
        {
            string Guid = null;

            using (var conn = new SnowflakeDbConnection())
            {
                _logger.LogInformation($"Trying to connect to SnowFlake database.");

                conn.ConnectionString = _snowFlakeSettings.ConnectionString;

                var cmdVc = CreateCommand(conn, request,_snowFlakeSettings.StatusPhoneQuery, _snowFlakeSettings.StatusEmailQuery);
                conn.Open();

                var rdVc = await cmdVc.ExecuteScalarAsync(cancellationToken);
                if (rdVc != null)
                {
                    Guid = Convert.ToString(rdVc);
                }

                if (string.IsNullOrWhiteSpace(Guid) && _snowFlakeSettings.UseRelaxed == "1")
                {
                    //prepare for call to relaxed...
                    cmdVc = CreateCommand(conn, request, _snowFlakeSettings.RelaxedPhoneQuery, _snowFlakeSettings.RelaxedEmailQuery);
                    
                    rdVc = await cmdVc.ExecuteScalarAsync(cancellationToken);
                    if (rdVc != null)
                    {
                        Guid = Convert.ToString(rdVc);
                    }
                }
            }

            return Guid;
        }
        public async Task<string> GetVaccineCredentialStatusAsync(SnowflakeDbConnection conn, GetVaccineCredentialStatusQuery request, CancellationToken cancellationToken)
        {
            string Guid = null;

            var cmdVc = CreateCommand(conn, request, _snowFlakeSettings.StatusPhoneQuery, _snowFlakeSettings.StatusEmailQuery);

            var rdVc = await cmdVc.ExecuteScalarAsync(cancellationToken);
            if (rdVc != null)
            {
                Guid = Convert.ToString(rdVc);
            }
            if (string.IsNullOrWhiteSpace(Guid) && _snowFlakeSettings.UseRelaxed == "1")
            {
                //prepare for call to relaxed...
                cmdVc = CreateCommand(conn, request, _snowFlakeSettings.RelaxedPhoneQuery, _snowFlakeSettings.RelaxedEmailQuery);
                
                rdVc = await cmdVc.ExecuteScalarAsync(cancellationToken);
                if (rdVc != null)
                {
                    Guid = Convert.ToString(rdVc);
                }
            }
            return Guid;
        }

        #endregion

        private DbCommand CreateCommand(SnowflakeDbConnection conn, GetVaccineCredentialStatusQuery request, string phoneQuery, string emailQuery)
        {
            var cmdVc = conn.CreateCommand();

            AddParameter(cmdVc, "1", request.FirstName.ToUpper().Trim());
            AddParameter(cmdVc, "2", request.LastName.ToUpper().Trim());
            AddParameter(cmdVc, "3", request.DateOfBirth?.ToString("yyyy-MM-dd"));

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber.Trim()))
            {
                AddParameter(cmdVc, "4", request.PhoneNumber.Trim());
                cmdVc.CommandText = phoneQuery;
            }
            else
            {
                AddParameter(cmdVc, "4", request.EmailAddress.ToLower().Trim());
                cmdVc.CommandText = emailQuery;
            }
            return cmdVc;
        }

        private static void AddParameter(DbCommand cmdVc, string name, string value)
        {
            var tempP = cmdVc.CreateParameter();
            tempP.ParameterName = name;
            tempP.Value = value;
            tempP.DbType = System.Data.DbType.String;
            cmdVc.Parameters.Add(tempP);
        }
    }
}
