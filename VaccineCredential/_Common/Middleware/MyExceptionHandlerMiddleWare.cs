using System;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace VaccineCredential.Api.Common.Middleware
{
    public class MyExceptionHandlerMiddleWare
    {
        private readonly RequestDelegate _next;

        #region Constructors
        public MyExceptionHandlerMiddleWare(RequestDelegate next)
        {
            _next = next;
        }
        #endregion

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = StatusCodes.Status500InternalServerError;
            var result = string.Empty;

            switch (exception)
            {
                case ValidationException validationException:
                    code = StatusCodes.Status422UnprocessableEntity;
                    result = JsonConvert.SerializeObject(validationException.Failures, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                    break;
                case BadRequestException badRequestException:
                    code = StatusCodes.Status400BadRequest;
                    result = badRequestException.Message;
                    break;
                case NotFoundException _:
                    code = StatusCodes.Status404NotFound;
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = code;

            if (result == string.Empty)
            {
                result = JsonConvert.SerializeObject(new { error = exception.Message });
            }

            return context.Response.WriteAsync(result);
        }
    }

    public static class MyExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseMyExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MyExceptionHandlerMiddleWare>();
        }
    }
}
