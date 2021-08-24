using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors
{
    public class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger _logger;
        private readonly ICurrentUserService _currentUserService;

        public RequestValidationBehavior(ILogger<TResponse> logger, IEnumerable<IValidator<TRequest>> validators, ICurrentUserService currentUserService)
        {
            _validators = validators;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                if (failures.Count != 0)
                {
                    var name = typeof(TRequest).Name;
                    _logger.LogWarning("Validation Error: {RequestName} {@CurrentUser} {@Request} {@Failures} ", name, _currentUserService, request, failures);
                    throw new Exceptions.ValidationException(failures);
                }
            }
            return await next();
        }
    }
}
