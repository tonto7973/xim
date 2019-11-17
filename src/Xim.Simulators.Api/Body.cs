using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Api response body.
    /// </summary>
    public abstract class Body : IDisposable
    {
        private bool _disposed;
        private bool _ownsDisposable;

        /// <summary>
        /// Gets the content representing the body of the response.
        /// </summary>
        public object Content { get; }

        /// <summary>
        /// Gets the Content-Type header for the response.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Body"/> class.
        /// </summary>
        /// <param name="content">The content representing the response body.</param>
        /// <param name="contentType">The Content-Type header for the response.</param>
        protected Body(object content, string contentType)
        {
            Content = content;
            ContentType = contentType;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Body"/> with a string body.
        /// </summary>
        /// <param name="httpBody">The <see cref="string"/> representing the response body.</param>
        /// <param name="encoding">Optional encoding. The default is <see cref="Encoding.UTF8"/>.</param>
        /// <param name="mediaType">Optional media type. The default is "text/plain".</param>
        /// <returns>The newly created response <see cref="Body"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="httpBody"/> is null.</exception>
        public static Body FromString(string httpBody, Encoding encoding = null, string mediaType = null)
            => InternalFromString(
                   httpBody ?? throw new ArgumentNullException(nameof(httpBody)),
                   encoding ?? Encoding.UTF8,
                   mediaType ?? "text/plain"
               );

        /// <summary>
        /// Creates a new instance of <see cref="Body"/> with a stream body.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the response body.</param>
        /// <param name="contentType">Content-Type header for the response. The default is <c>"application/octet-stream"</c>.</param>
        /// <param name="leaveOpen">true to leave the <paramref name="stream"/> open after the <see cref="Body"/> is written; otherwise, false.</param>
        /// <returns>The newly created response <see cref="Body"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is null.</exception>
        public static Body FromStream(Stream stream, string contentType = null, bool leaveOpen = false)
            => new Body<Stream>(stream ?? throw new ArgumentNullException(nameof(stream)), contentType)
            {
                _ownsDisposable = !leaveOpen
            };

        /// <summary>
        /// Creates a new instance of <see cref="Body"/> with an object.
        /// </summary>
        /// <typeparam name="TContent">The type of the body <paramref name="value"/>.</typeparam>
        /// <param name="value">The object representing the body.</param>
        /// <param name="encoding"></param>
        /// <returns>The newly created response <see cref="Body"/>.</returns>
        public static Body FromObject<TContent>(TContent value, Encoding encoding = null)
            => new Body<TContent>(value, encoding);

        /// <summary> 
        /// Writes the body to the response stream. 
        /// </summary> 
        /// <param name="context">The <see cref="HttpContext"/>.</param> 
        /// <param name="settings">The <see cref="ApiSimulatorSettings"/>.</param> 
        /// <returns>A task that represents the asynchronous write operation.</returns>
        protected abstract Task WriteAsync(HttpContext context, ApiSimulatorSettings settings);

        internal Task InternalWriteAsync(HttpContext context, ApiSimulatorSettings settings)
            => WriteAsync(context, settings);

        /// <summary>
        /// Releases all resources currently used by this <see cref="Body"/> instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method is being invoked by the <see cref="Dispose()"/> method,
        /// otherwise <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && _ownsDisposable)
            {
                (Content as IDisposable)?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Releases resources used by this <see cref="Body"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static Body<Stream> InternalFromString(string httpBody, Encoding encoding, string mediaType)
            => new Body<Stream>(new MemoryStream(encoding.GetBytes(httpBody)), mediaType + "; charset=" + encoding.HeaderName)
            {
                _ownsDisposable = true
            };
    }
}
