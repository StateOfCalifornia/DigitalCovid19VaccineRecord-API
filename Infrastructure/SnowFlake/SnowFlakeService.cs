using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;
using Application.VaccineCredential.Queries.GetVaccineStatus;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using Newtonsoft.Json;

namespace Infrastructure.SnowFlake
{
    public class SnowFlakeService : ISnowFlakeService
    {
        private readonly ILogger<SnowFlakeService> _logger;
        private readonly SnowFlakeSettings _snowFlakeSettings;
        private static AzureToken _oAuthToken = new();
        #region Constructor

        public SnowFlakeService(ILogger<SnowFlakeService> logger, SnowFlakeSettings snowFlakeSettings)
        {
            _logger = logger;
            _snowFlakeSettings = snowFlakeSettings;
        }

        #endregion
        class VcObject
        {
            [JsonProperty("vc")]
            public Vc Vc { get; set; }
        }

        #region ISnowFlakeService Implementation
        public async Task<Vc> GetVaccineCredentialSubjectAsync(string id, CancellationToken cancellationToken)
        {
            Vc vaccineCredential = null;
            try
            {
                using (var conn = new SnowflakeDbConnection())
                {
                    _logger.LogInformation($"Trying to connect to SnowFlake database.");

                    conn.ConnectionString = GetSnowFlakeConnectionString();
                    var cmdVc = conn.CreateCommand();

                    cmdVc.CommandText = _snowFlakeSettings.IdQuery;

                    conn.Open();

                    AddParameter(cmdVc, "1", id);

                    var rdVc = await cmdVc.ExecuteReaderAsync(cancellationToken);
                    if (await rdVc.ReadAsync(cancellationToken))
                    {
                        var jsonString = rdVc.GetString(0);
                        var vaccineCredentialobject = JsonConvert.DeserializeObject<VcObject>(jsonString);
                        vaccineCredential = vaccineCredentialobject.Vc;
                    }
                }
            }
            catch (Exception ex)
            {
                _oAuthToken = new AzureToken();
                _logger.LogError("Failure in accessing Snowflake. Error:{0}", ex.Message);
                throw new Exception("Unable to connect to database");
            }

            return vaccineCredential;
        }

        public async Task<string> GetVaccineCredentialStatusAsync(GetVaccineCredentialStatusQuery request, CancellationToken cancellationToken)
        {
            string Guid = null;

            try
            {
                using (var conn = new SnowflakeDbConnection())
                {
                    _logger.LogInformation($"Trying to connect to SnowFlake database.");

                    conn.ConnectionString = GetSnowFlakeConnectionString();

                    var cmdVc = CreateCommand(conn, request, _snowFlakeSettings.StatusPhoneQuery,
                        _snowFlakeSettings.StatusEmailQuery);
                    conn.Open();

                    var rdVc = await cmdVc.ExecuteScalarAsync(cancellationToken);
                    if (rdVc != null)
                    {
                        Guid = Convert.ToString(rdVc);
                    }

                    if (string.IsNullOrWhiteSpace(Guid) && _snowFlakeSettings.UseRelaxed == "1")
                    {
                        //prepare for call to relaxed...
                        cmdVc = CreateCommand(conn, request, _snowFlakeSettings.RelaxedPhoneQuery,
                            _snowFlakeSettings.RelaxedEmailQuery);

                        rdVc = await cmdVc.ExecuteScalarAsync(cancellationToken);
                        if (rdVc != null)
                        {
                            Guid = Convert.ToString(rdVc);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _oAuthToken = new AzureToken();
                _logger.LogError("Failure in accessing Snowflake. Error:{0}", ex.Message);
                throw new Exception("Unable to connect to database");
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

        public string GetSnowFlakeConnectionString()
        {
            var conn = _snowFlakeSettings.ConnectionString;

            if (!conn.Contains("<​​​oauthTokenValue>")) return conn;

            var token = GetOAuthToken();
            conn = conn.Replace("<​​​oauthTokenValue>", token.AccessToken);
            return conn;
        }
        #endregion

        private AzureToken GetOAuthToken()
        {
            if (!string.IsNullOrWhiteSpace(_oAuthToken.AccessToken) && _oAuthToken.ExpiryDate > DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)))
            {
                return _oAuthToken; //If the token is not expired within 10 minutes, do not request new token.
            }

            using var httpClient = new HttpClient();
            var url = _snowFlakeSettings.MicrosoftOAuthUrl;

            var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", _snowFlakeSettings.ClientId),
                    new KeyValuePair<string, string>("client_secret", _snowFlakeSettings.ClientSecret),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", _snowFlakeSettings.UserName),
                    new KeyValuePair<string, string>("password", _snowFlakeSettings.ClientPassword),
                    new KeyValuePair<string, string>("scope", _snowFlakeSettings.Scope)
                };

            var content = new FormUrlEncodedContent(values);

            var result = httpClient.PostAsync(url, content).Result;

            if (result.IsSuccessStatusCode)
            {
                var resultString = result.Content.ReadAsStringAsync().Result;
                _oAuthToken = JsonConvert.DeserializeObject<AzureToken>(resultString);

                if (_oAuthToken != null)
                {
                    _oAuthToken.ExpiryDate = new JwtSecurityToken(_oAuthToken.AccessToken).ValidTo;
                }
            }

            return _oAuthToken;
        }
        private static DbCommand CreateCommand(SnowflakeDbConnection conn, GetVaccineCredentialStatusQuery request, string phoneQuery, string emailQuery)
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

    public class AzureToken
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("scope")]
        public string Scope { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonProperty("ext_expires_in")]
        public int ExtExpiresIn { get; set; }
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
