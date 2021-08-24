using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors
{
    public class RequestPerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest,TResponse>
    {
        private const int _threshold = 500;
        private readonly Stopwatch _timer;
        private readonly ILogger<TRequest> _logger;
        private readonly ICurrentUserService _currentUserService;

        public RequestPerformanceBehavior(ILogger<TRequest> logger, ICurrentUserService currentUserService)
        {
            _timer = new Stopwatch();
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _timer.Start();

            var response = await next();

            _timer.Stop();

            if (_timer.ElapsedMilliseconds > _threshold)
            {
                var name = typeof(TRequest).Name;
                _logger.LogWarning("Long Running Request: {RequestName} {@CurrentUser} {@Request} (Limit Threshold is {LimitThresholdInMilliseconds} milliseconds):  ({ElapsedMilliseconds} milliseconds)",
                    name, _currentUserService, request, _threshold, _timer.ElapsedMilliseconds);
            }

            return response;
        }
    }
}
