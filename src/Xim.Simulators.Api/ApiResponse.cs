using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Represents the api response.
    /// </summary>
    public sealed class ApiResponse : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Gets HTTP response status code.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the response reason phrase. Can be null.
        /// </summary>
        public string ReasonPhrase { get; }

        /// <summary>
        /// Gets the response <see cref="Headers"/>.
        /// </summary>
        public Headers Headers { get; }

        /// <summary>
        /// Gets the response <see cref="Body"/>. Can be null.
        /// </summary>
        public Body Body { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ApiResponse"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP response status code.</param>
        /// <param name="reasonPhrase">Optional HTTP response reason phrase.</param>
        /// <param name="headers">Optional HTTP response headers.</param>
        /// <param name="body">Optional HTTP response body.</param>
        /// <remarks>If the headers parameter is null, it will be replaced with a new instance of <see cref="Api.Headers"/></remarks>
        public ApiResponse(int statusCode, string reasonPhrase = null, Headers headers = null, Body body = null)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Headers = headers ?? new Headers();
            Body = body;
        }

        internal Task WriteAsync(HttpContext context, ApiSimulatorSettings settings)
        {
            context.Response.StatusCode = StatusCode;
            WriteReasonPhrase(context);
            WriteHeaders(context);
            return WriteBodyAsync(context, settings);
        }

        private void WriteReasonPhrase(HttpContext context)
        {
            if (ReasonPhrase == null)
                return;
            context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = ReasonPhrase;
        }

        private void WriteHeaders(HttpContext context)
        {
            foreach (var header in Headers)
            {
                context.Response.Headers.Append(header.Key, header.Value);
            }
        }

        private Task WriteBodyAsync(HttpContext context, ApiSimulatorSettings settings)
            => Body == null
                ? Task.CompletedTask
                : Body.InternalWriteAsync(context, settings);

        /// <summary>
        /// Releases managed resources used by the <see cref="ApiResponse"/>.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Body?.Dispose();

            _disposed = true;
        }

        /// <summary>
        /// Creates a HTTP 200 Ok response.
        /// </summary>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Ok(Body body = null)
            => new ApiResponse(200, body: body);

        /// <summary>
        /// Creates a HTTP 200 Ok response.
        /// </summary>
        /// <param name="headers">Response headers.</param>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Ok(Headers headers, Body body = null)
            => new ApiResponse(200, headers: headers, body: body);

        /// <summary>
        /// Creates a HTTP 200 Ok response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="body">The response body object.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Ok<TBody>(TBody body)
            => new ApiResponse(200, body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 200 Ok response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="headers">Response headers.</param>
        /// <param name="body">The response body object.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Ok<TBody>(Headers headers, TBody body)
            => new ApiResponse(200, headers: headers, body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 201 Created response.
        /// </summary>
        /// <param name="location">The created resource location.</param>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Created(Uri location, Body body = null)
            => new ApiResponse(201, headers: new Headers().AddLocation(location), body: body);

        /// <summary>
        /// Creates a HTTP 201 Created response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="location">The created resource location.</param>
        /// <param name="body">The response body object.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Created<TBody>(Uri location, TBody body)
            => new ApiResponse(201, headers: new Headers().AddLocation(location), body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 202 Accepted response.
        /// </summary>
        /// <param name="location">The location at which the status of requested resource can be monitored.</param>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Accepted(Uri location, Body body = null)
            => new ApiResponse(202, headers: new Headers().AddLocation(location), body: body);

        /// <summary>
        /// Creates a HTTP 202 Accepted response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="location">The location at which the status of requested resource can be monitored.</param>
        /// <param name="body">The response body object.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Accepted<TBody>(Uri location, TBody body)
            => new ApiResponse(202, headers: new Headers().AddLocation(location), body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 400 BadRequest response.
        /// </summary>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse BadRequest(Body body = null)
            => new ApiResponse(400, body: body);

        /// <summary>
        /// Creates a HTTP 400 BadRequest response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="body">The response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse BadRequest<TBody>(TBody body)
            => new ApiResponse(400, body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 401 Unauthorized response.
        /// </summary>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Unauthorized(Body body = null)
            => new ApiResponse(401, body: body);

        /// <summary>
        /// Creates a HTTP 401 Unauthorized response with an authenticate challenge.
        /// </summary>
        /// <param name="challenge">The unauthorized challenge for the WWW-Authenticate header, i.e. "Basic".</param>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Unauthorized(string challenge, Body body = null)
            => new ApiResponse(401, headers: new Headers().AddWwwAuthenticate(challenge), body: body);

        /// <summary>
        /// Creates a HTTP 401 Unauthorized response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="body">The response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Unauthorized<TBody>(TBody body)
            => new ApiResponse(401, body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 401 Unauthorized response with an authenticate challenge.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="challenge">The unauthorized challenge for the WWW-Authenticate header, i.e. "Basic".</param>
        /// <param name="body">The response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Unauthorized<TBody>(string challenge, TBody body)
            => new ApiResponse(401, headers: new Headers().AddWwwAuthenticate(challenge), body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 403 Forbidden response.
        /// </summary>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Forbidden(Body body = null)
            => new ApiResponse(403, body: body);

        /// <summary>
        /// Creates a HTTP 403 Forbidden response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="body">The response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse Forbidden<TBody>(TBody body)
            => new ApiResponse(403, body: Body.FromObject(body));

        /// <summary>
        /// Creates a HTTP 404 NotFound response.
        /// </summary>
        /// <param name="body">Optional response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse NotFound(Body body = null)
            => new ApiResponse(404, body: body);

        /// <summary>
        /// Creates a HTTP 404 NotFound response.
        /// </summary>
        /// <typeparam name="TBody">The type of the body response.</typeparam>
        /// <param name="body">The response body.</param>
        /// <returns>The <see cref="ApiResponse"/>.</returns>
        public static ApiResponse NotFound<TBody>(TBody body)
            => new ApiResponse(404, body: Body.FromObject(body));
    }
}
