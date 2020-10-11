using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xim.Simulators.Api.Internal;

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
            context.SetApiSimulatorSettings(_settings);
            var action = context.Request.Method
                + " " + context.Request.Path.Value
                + (context.Request.QueryString.Value ?? "");
            _logger.LogDebug($"Request \"{action}\" started");
            Stream requestBodyStream = await OverrideRequestBodyAsync(context.Request).ConfigureAwait(false);
            var apiCall = ApiCall.Start(action, context);
            try
            {
                ApiHandler handler = _settings.Handlers.Next(action) ?? _settings.DefaultHandler ?? EmptyHandler;
                ApiResponse response = await handler(context).ConfigureAwait(false) ?? new ApiResponse(502, "Invalid Handler");
                using (response)
                {
                    await response
                        .WriteAsync(context, _settings)
                        .ConfigureAwait(false);
                }
                apiCall.Succeed(response);
                _logger.LogDebug($"Request \"{action}\" succeeded");
            }
            catch (Exception exception)
            {
                apiCall.Fail(exception);
                _logger.LogError(exception, $"Error processing request \"{action}\"");
                throw;
            }
            finally
            {
                (context.Request.Body as MemoryStream)?.Seek(0, SeekOrigin.Begin);
                context.Request.Body = requestBodyStream;
                _apiCall.Invoke(apiCall.Stop());
                _logger.LogDebug($"Request \"{action}\" completed");
            }
        }

        private static async Task<Stream> OverrideRequestBodyAsync(HttpRequest request)
        {
            Stream oldBody = request.Body;
            if (oldBody != null)
            {
                var newBody = new MemoryStream();
                await Body
                    .CopyBytesAsync(oldBody, newBody, request.ContentLength)
                    .ConfigureAwait(false);
                newBody.Position = 0;
                request.Body = newBody;
            }
            return oldBody;
        }
    }
}
