using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Xim.Simulators.Api
{
    internal class ApiSimulatorOwinMiddleware : IMiddleware
    {
        private static readonly ApiHandler EmptyHandler = _ => Task.FromResult<ApiResponse>(null);

        private readonly ApiSimulatorSettings _settings;
        private readonly ILogger _logger;
        private readonly Action<ApiCall> _apiCall;

        public ApiSimulatorOwinMiddleware(ApiSimulatorSettings settings, ILogger logger, Action<ApiCall> apiCall)
        {
            _settings = settings;
            _logger = logger;
            _apiCall = apiCall;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var action = context.Request.Method
                + " " + context.Request.Path.Value
                + (context.Request.QueryString.Value ?? "");
            _logger.LogDebug($"Request \"{action}\" started");
            var apiCall = ApiCall.Start(action, context);
            try
            {
                var handler = _settings.Handlers[action] ?? _settings.DefaultHandler ?? EmptyHandler;
                var response = await handler(context).ConfigureAwait(false) ?? new ApiResponse(502, "Invalid Handler");
                using (response)
                {
                    await response
                        .WriteAsync(context, _settings)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                apiCall.Fail(exception);
                _logger.LogError(exception, $"Error processing request \"{action}\"");
                throw;
            }
            finally
            {
                _apiCall.Invoke(apiCall.Stop());
                _logger.LogDebug($"Request \"{action}\" completed");
            }
        }
    }
}
