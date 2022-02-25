using Application.Common.Interfaces;
using Application.Options;
using Infrastructure;
using Infrastructure.SnowFlake;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialServiceJob
{
    public class Startup
    {

        public static IServiceCollection ConfigureService()
        {
            var services = ConfigureServices();

            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            var builder = new ConfigurationBuilder();
            // tell the builder to look for the appsettings.json file
            builder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            builder.AddUserSecrets<Program>();

            Configuration = builder.Build();
            services
                 .AddTransient<IStartupFilter, OptionsValidationStartupFilter>()

                 .Configure<SendGridSettings>(o => Configuration.GetSection("SendGridSettings").Bind(o))
                 .Configure<SnowFlakeSettings>(o => Configuration.GetSection("SnowFlakeSettings").Bind(o))
                 .Configure<AppSettings>(o => Configuration.GetSection("AppSettings").Bind(o))
                 .Configure<TwilioSettings>(o => Configuration.GetSection("TwilioSettings").Bind(o))
                 .Configure<MessageQueueSettings>(o => Configuration.GetSection("MessageQueueSettings").Bind(o))
                 .Configure<KeySettings>(o => Configuration.GetSection("KeySettings").Bind(o))
                 .Configure<CdphMessageSettings>(o => Configuration.GetSection("CDPHMessageSettings").Bind(o))
                 .Configure<PinpointEmailSettings>(o => Configuration.GetSection("PinpointEmailSettings").Bind(o))
                 .AddOptions()
                 .AddLogging(configure => configure.AddApplicationInsightsWebJobs(c => c.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"))
                .AddConsole(configure =>
                 {
                     // print out only 1 line per message( without this 2 are printed)
                     configure.FormatterName = ConsoleFormatterNames.Systemd;
                 }))

                 .AddSingleton(r => r.GetRequiredService<IOptions<SendGridSettings>>().Value)
                 .AddSingleton(r => r.GetRequiredService<IOptions<SnowFlakeSettings>>().Value)
                 .AddSingleton(r => r.GetRequiredService<IOptions<AppSettings>>().Value)
                 .AddSingleton(r => r.GetRequiredService<IOptions<TwilioSettings>>().Value)
                 .AddSingleton(r => r.GetRequiredService<IOptions<MessageQueueSettings>>().Value)
                 .AddSingleton(r => r.GetRequiredService<IOptions<KeySettings>>().Value)
                 .AddSingleton(r => r.GetRequiredService<IOptions<CdphMessageSettings>>().Value)
                 .AddSingleton(r => r.GetRequiredService<IOptions<PinpointEmailSettings>>().Value)

                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<SendGridSettings>>().Value)
                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<SnowFlakeSettings>>().Value)
                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<AppSettings>>().Value)
                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<TwilioSettings>>().Value)
                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<MessageQueueSettings>>().Value)
                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<KeySettings>>().Value)
                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<CdphMessageSettings>>().Value)
                 .AddSingleton<ISettingsValidate>(r => r.GetRequiredService<IOptions<PinpointEmailSettings>>().Value)

                 .AddSingleton<IEmailService, EmailService>()
                 .AddSingleton<IBase64UrlUtility, Base64UrlUtility>()
                 .AddSingleton<IAesEncryptionService, AesEncryptionService>()
                 .AddSingleton<IAesEncryptionService, AesEncryptionService>()
                 .AddSingleton<ISnowFlakeService, SnowFlakeService>()
                 .AddSingleton<IMessagingService, MessagingService>()
                 .AddSingleton<IQueueProcessor, Program>()
                 .BuildServiceProvider();

            AddSendGridClient(services);
            AddCdphSmsMessagingClient(services);
            AddPinpointEmailClient(services);
            return services;
        }

        private static void AddSendGridClient(IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetService<IOptions<SendGridSettings>>().Value;

            //Initialize and add context
            var client = new SendGridClient(options.Key);

            services.AddTransient(x => client);
        }

        private static void AddPinpointEmailClient(IServiceCollection services)
        {
            //Register the CdphMessaging Client and add necessary headers
            services.AddHttpClient<PinpointEmailClient>(client =>
            {
                var options = services.BuildServiceProvider().GetService<PinpointEmailSettings>();
                client.BaseAddress = new Uri(options.EmailApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-api-key", options.EmailKey);
            });
        }

        private static void AddCdphSmsMessagingClient(IServiceCollection services)
        {
            //Register the CdphMessaging Client and add necessary headers
            services.AddHttpClient<CdphSmsMessagingClient>(client =>
            {
                var options = services.BuildServiceProvider().GetService<CdphMessageSettings>();
                client.BaseAddress = new Uri(options.SmsApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-api-key", options.SmsKey);
            });
        }

        private static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            var config = LoadConfiguration();
            services.AddSingleton(config);

            // required to run the application
            services.AddTransient<EmailService>();
            services.AddTransient<Base64UrlUtility>();
            services.AddTransient<SnowFlakeService>();
            services.AddTransient<AesEncryptionService>();


            return services;
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>();

            return builder.Build();
        }

    }
}
