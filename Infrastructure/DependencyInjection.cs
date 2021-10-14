using Application.Common.Interfaces;
using Application.Options;
using Infrastructure.SnowFlake;
using Infrastructure.QrApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SendGrid;
using Twilio;
using Microsoft.AspNetCore.Hosting;
using Azure.Storage.Queues;
using System;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMyInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Infrastructure uses caching
            services.AddMemoryCache();

            // Bind all Infrastructure configuration settings
            services.Configure<SnowFlakeSettings>(o => configuration.GetSection("SnowFlakeSettings").Bind(o));
            services.Configure<TwilioSettings>(o => configuration.GetSection("TwilioSettings").Bind(o));
            services.Configure<SendGridSettings>(o => configuration.GetSection("SendGridSettings").Bind(o));
            services.Configure<KeySettings>(o => configuration.GetSection("KeySettings").Bind(o));
            services.Configure<MessageQueueSettings>(o => configuration.GetSection("MessageQueueSettings").Bind(o));
            services.Configure<AppSettings>(o => configuration.GetSection("AppSettings").Bind(o));

            // Explicitly register the Infrastructure configuration setting objects by delegating to the IOptions object
            // This will allow us to DI the settings directly, without IOptions
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SnowFlakeSettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<TwilioSettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SendGridSettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<KeySettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<MessageQueueSettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);

            // Register IOptionsValidatables in order to validate all settings
            services.AddSingleton<ISettingsValidate>(resolver => resolver.GetRequiredService<IOptions<SnowFlakeSettings>>().Value);
            services.AddSingleton<ISettingsValidate>(resolver => resolver.GetRequiredService<IOptions<TwilioSettings>>().Value);
            services.AddSingleton<ISettingsValidate>(resolver => resolver.GetRequiredService<IOptions<SendGridSettings>>().Value);
            services.AddSingleton<ISettingsValidate>(resolver => resolver.GetRequiredService<IOptions<KeySettings>>().Value);
            services.AddSingleton<ISettingsValidate>(resolver => resolver.GetRequiredService<IOptions<MessageQueueSettings>>().Value);
            services.AddSingleton<ISettingsValidate>(resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);


            AddSendGridClient(services);
            AddQueryClient(services);
            AddQrApiService(services);
            // Register service objects
            services.AddTransient<ISnowFlakeService, SnowFlakeService>();
            services.AddTransient<IMessagingService, MessagingService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IJwtSign, JwtSign>();
            services.AddTransient<IJwtChunk, JwtChunk>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddTransient<ICompact, Compact>();
            services.AddTransient<IRateLimitService, RateLimitService>();
            services.AddTransient<ICredentialCreator, CredentialCreator>();
            services.AddTransient<IBase64UrlUtility, Base64UrlUtility>();
            services.AddTransient<IAesEncryptionService, AesEncryptionService>();
            services.AddTransient<IDateTime, MachineDateTime>();

            return services;
        }

        #region Private Helpers
        private static void AddSendGridClient(IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetService<IOptions<SendGridSettings>>().Value;

            //Initialize and add context
            var client = new SendGridClient(options.Key);

            services.AddTransient(x => client);
        }
        private static void AddQueryClient(IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetService<IOptions<MessageQueueSettings>>().Value;

            //Initialize and add context
            var qOptions = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            var clientCredential = new QueueClient(options.ConnectionString, options.QueueName,  qOptions);
            var clientAlternate = new QueueClient(options.ConnectionString, options.AlternateQueueName, qOptions);

            services.AddSingleton(x => clientCredential);
            services.AddSingleton(x => clientAlternate);
        }
        private static void AddQrApiService(IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetService<IOptions<AppSettings>>().Value;
            if(!String.IsNullOrEmpty(options.QrCodeApi)){
                services.AddTransient<IQrApiService, QrApiService>();
            }else{
                services.AddTransient<IQrApiService, QrService>();
            }
            
        }
        #endregion
    }
}
