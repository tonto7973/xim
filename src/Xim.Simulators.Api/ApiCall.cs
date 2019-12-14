using System;
using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Represents an api call that records request, response and optional error captured during
    /// handlind an api request.
    /// </summary>
    public sealed class ApiCall
    {
        /// <summary>
        /// Gets the call id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the recorded action, such as <c>"GET /index/data"</c>.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Gets the http request recorded during the call.
        /// </summary>
        public ApiRequest Request { get; }

        /// <summary>
        /// Gets the http response recorded duting the call.
        /// </summary>
        public ApiResponse Response { get; private set; }

        /// <summary>
        /// Gets the <see cref="Exception"/> recorded during the api call. Can be null.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets the UTC <see cref="DateTime"/>when this call started.
        /// </summary>
        public DateTime StartTimeUtc { get; }

        /// <summary>
        /// Gets the duration of this call.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        private ApiCall(string action, HttpContext context)
        {
            StartTimeUtc = DateTime.UtcNow;
            Id = context.TraceIdentifier;
            Action = action;
            Request = new ApiRequest(context.Request);
        }

        internal static ApiCall Start(string action, HttpContext context)
            => new ApiCall(action, context);

        internal void Succeed(ApiResponse response)
            => Response = response;

        internal void Fail(Exception exception)
            => Exception = exception;

        internal ApiCall Stop()
        {
            Duration = DateTime.UtcNow - StartTimeUtc;
            return this;
        }
    }
}
