using System;
using System.Collections.Generic;
using System.Reflection;
using FluentValidation;
using Application.Common.Behaviors;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMyApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPerformanceBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));

            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);           
            return services;
        }
    }
}
