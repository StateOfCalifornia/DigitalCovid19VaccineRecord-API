using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace InfrastructureTests
{
    internal class ConfigUtilities
    {
        //private const string EditorRoleName = "Editors";
        private static IConfiguration _configuration;
        private static IConfigurationSection _config;

        public static string GetConfigValue(string keyName)
        {
            if (_config == null) _config = GetConfig().GetSection("secrets");
            var value = _config[keyName];

            return value;
        }

        internal static IConfiguration GetConfig()
        {
            if (_configuration != null) return _configuration;

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonFile(jsonConfig, true) // Lowest priority - put here
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true); // User secrects override all - put here

            var config = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(2), 10);

            // From either local secrets or app config, get connection info for Azure Vault.
            var clientId = config["ClientId"];
            if (string.IsNullOrEmpty(clientId)) clientId = Environment.GetEnvironmentVariable("CLIENTID");

            var key = config["Key"];
            if (string.IsNullOrEmpty(key)) key = Environment.GetEnvironmentVariable("KEY");

           

            _configuration = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(2), 10);

            return _configuration;
        }
    }
}