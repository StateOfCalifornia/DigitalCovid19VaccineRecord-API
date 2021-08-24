using System.Linq;
using System.Text;
using Application;
using Application.Common.Interfaces;
using AutoMapper;
using FluentValidation.AspNetCore;
using Infrastructure;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VaccineCredential.Api.Common.Extensions;
using VaccineCredential.Api.Common.Filters;
using VaccineCredential.Api.Common.Middleware;
using VaccineCredential.Api.Options;
using VaccineCredential.Api.Services;

namespace VaccineCredential.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        private IServiceCollection _services;

        #region Constructor
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }
        #endregion

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (Environment.IsDevelopment())
            {
                services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                }));
            }


            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration =  System.Environment.GetEnvironmentVariable("AppSettings:RedisConnectionString");
                options.InstanceName = "main";
            });
            // Add Option settings Startup filter in order to help validate Setting configs
            // This will only validate at startup. Will not validate if changed afterwards
            services.AddTransient<IStartupFilter, OptionsValidationStartupFilter>();

            // Bind all Api related configuration settings
            services.Configure<SwaggerSettings>(o => Configuration.GetSection("SwaggerSettings").Bind(o));

            // Explicitly register the Api configuration setting objects by delegating to the IOptions object
            // This will allow us to DI the settings directly, without IOptions
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SwaggerSettings>>().Value);

            //Register Application Insights.
            services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetry>();

            var aiOptions = new ApplicationInsightsServiceOptions
            {
                EnableAdaptiveSampling = false,
                EnableQuickPulseMetricStream = false,
                EnableAuthenticationTrackingJavaScript = false
            };

            services.AddApplicationInsightsTelemetry(Configuration);

            // Add Project Dependencies
            services.AddMyInfrastructure(Configuration);
            services.AddMyApplication(Configuration);

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddHttpContextAccessor();

            services
                .AddControllers(options =>
                {
                    //All routes possibly can throw these errors by default, 
                    //so lets document them so they are added/documented in swagger to each route by default
                    //All routes possibly can throw these errors, so lets document them so they are added/documented to each route by default
                    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(string), StatusCodes.Status400BadRequest));
                    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(string), StatusCodes.Status406NotAcceptable));
                    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(string), StatusCodes.Status500InternalServerError));
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.Formatting = !Environment.IsProduction() ? Formatting.Indented : Formatting.None;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                })
                .AddFluentValidation();

            // Customise default API behaviour to supress modlestate as FluentValidation takes care of this
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddMySwaggerDocumentation(Configuration);

            //The middleware defaults to sending a Status307TemporaryRedirect with all redirects
            //We want to enable a permanent redirect
            if (!Environment.IsDevelopment())
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                    options.HttpsPort = 443;
                });
            }
            _services = services;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IMapper autoMapper)
        {
            //Verify Automapper Configuration
            autoMapper.ConfigurationProvider.AssertConfigurationIsValid();

            if (env.IsDevelopment())
            {
                app.UseCors("MyPolicy");
                app.UseDeveloperExceptionPage();
                RegisterMyServicesPage(app);
            }
            else
            {
                app.UseHsts();
            }

            app.UseStaticFiles();

            app.UseMyExceptionHandler();
            app.UseHttpsRedirection();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto
            });
            var netcoreEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(netcoreEnv) && !netcoreEnv.ToLower().Contains("production")) { 
                app.UseMySwaggerDocumentation(Configuration);
            }

            // (A)
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // (B)
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        #region Private Helpers
        private void RegisterMyServicesPage(IApplicationBuilder app)
        {
            app.Map("/services", builder => builder.Run(async context =>
            {
                var sb = new StringBuilder();
                sb.Append("<h1>Registered Services</h1>");
                sb.Append("<table><thead>");
                sb.Append("<tr><th>Type</th><th>Lifetime</th><th>Instance</th></tr>");
                sb.Append("</thead><tbody>");
                foreach (var svc in _services.OrderBy(x => x.ServiceType.FullName))
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{svc.ServiceType.FullName}</td>");
                    sb.Append($"<td>{svc.Lifetime}</td>");
                    sb.Append($"<td>{svc.ImplementationType?.FullName}</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</tbody></table>");
                await context.Response.WriteAsync(sb.ToString());
            }));
        }

        
        #endregion
    }
}
