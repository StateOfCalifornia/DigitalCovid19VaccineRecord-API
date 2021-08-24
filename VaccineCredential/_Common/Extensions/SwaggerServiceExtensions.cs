using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using VaccineCredential.Api.Options;

namespace VaccineCredential.Api.Common.Extensions
{
    public static class SwaggerServiceExtensions
    {
        #region Public static Methods
        public static IServiceCollection AddMySwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
        {
            //Get swagger configuration settings
            var swaggerSettings = new SwaggerSettings();
            configuration.GetSection("SwaggerSettings").Bind(swaggerSettings);

            //Add Swagger
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc(swaggerSettings.DocumentName, new OpenApiInfo
                {
                    Version = swaggerSettings.Version,
                    Title = swaggerSettings.Title,
                    Description = $"{swaggerSettings.Description}.<br /><br />Environment: {configuration["BuildSettings:Properties:Environment"]}<br />Build Number: {configuration["BuildSettings:Properties:Version"]}<br />Running on: {RuntimeInformation.FrameworkDescription}",
                    TermsOfService = new Uri("http://www.dotnetdetail.net"),
                    Contact = new OpenApiContact
                    {
                        Name = swaggerSettings.Contact.Name,
                        Email = swaggerSettings.Contact.Email,
                        Url = new Uri(swaggerSettings.Contact.Url)
                    },
                    License = new OpenApiLicense
                    {
                        Name = "California Department of Technology",
                        Url = new Uri("http://www.dotnetdetail.net")
                    },
                });

                //Add Object documentation files
                var basePath = AppContext.BaseDirectory;
                s.IncludeXmlComments(Path.Combine(basePath, "VaccineCredential.Api.xml"));
                s.IncludeXmlComments(Path.Combine(basePath, "Application.xml"));
            });
            return services;
        }

        public static IApplicationBuilder UseMySwaggerDocumentation(this IApplicationBuilder app, IConfiguration configuration)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint and serving swagger at root url
            app.UseSwaggerUI(c =>
            {
                var swaggerOptions = new SwaggerSettings();
                configuration.GetSection("SwaggerSettings").Bind(swaggerOptions);
                var endpoint = $"/swagger/{swaggerOptions.DocumentName}/swagger.json";
                c.SwaggerEndpoint(endpoint, swaggerOptions.ProjectName);

                //This tells the api to serve swagger at root url
                c.RoutePrefix = string.Empty;
                c.DocumentTitle = swaggerOptions.ProjectName;
                c.DisplayRequestDuration();
                c.InjectStylesheet("/swagger/ui/custom.css");
            });
            return app;
        }
        #endregion
    }
}
