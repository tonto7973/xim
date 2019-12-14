using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Represents the api request.
    /// </summary>
    public sealed class ApiRequest
    {
        /// <summary>
        /// Gets the HTTP method.
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Gets the request path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the request query.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Gets the request headers.
        /// </summary>
        public Headers Headers { get; }

        /// <summary>
        /// Gets the request body.
        /// </summary>
        public Body Body { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ApiRequest"/>.
        /// </summary>
        internal ApiRequest(HttpRequest request)
        {
            Method = request.Method;
            Path = request.Path;
            Query = request.QueryString.Value;
            Headers = request.Headers == null ? null : Headers.FromHeaderDictionary(request.Headers);
            Body = request.Body == null ? null : Body.FromStream(request.Body, request.ContentType, true);
        }

        /// <summary>
        /// Returns a string representing the <see cref="ApiRequest"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> with representing the request.</returns>
        public override string ToString()
            => $"{Method} {Path}{Query}";
    }
}