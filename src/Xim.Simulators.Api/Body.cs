using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xim.Simulators.Api.Internal;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Api body.
    /// </summary>
    public abstract class Body : IDisposable
    {
        private bool _disposed;
        private bool _ownsDisposable;
        private ApiSimulatorSettings _settings;

        /// <summary>
        /// Gets the content representing the body.
        /// </summary>
        public object Content { get; }

        /// <summary>
        /// Gets the Content-Type header for the body.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets the Content-Length header for the body, if specified.
        /// </summary>
        public long? ContentLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Body"/> class.
        /// </summary>
        /// <param name="content">The content representing the body.</param>
        /// <param name="contentType">The Content-Type header for the body.</param>
        /// <param name="contentLength">Optional Content-Length header for the body.</param>
        protected Body(object content, string contentType, long? contentLength = null)
        {
            Content = content;
            ContentType = contentType;
            ContentLength = contentLength;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Body"/> with a string body.
        /// </summary>
        /// <param name="httpBody">The <see cref="string"/> representing the body.</param>
        /// <param name="encoding">Optional encoding. The default is <see cref="Encoding.UTF8"/>.</param>
        /// <param name="mediaType">Optional media type. The default is "text/plain".</param>
        /// <returns>The newly created <see cref="Body"/>.</returns>
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
        /// <param name="stream">The <see cref="Stream"/> representing the body.</param>
        /// <param name="contentType">Content-Type header for the body. The default is <c>"application/octet-stream"</c>.</param>
        /// <param name="contentLength">Optional Content-Length header for the body.</param>
        /// <param name="leaveOpen">true to leave the <paramref name="stream"/> open after the <see cref="Body"/> is written; otherwise, false.</param>
        /// <returns>The newly created <see cref="Body"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is null.</exception>
        public static Body FromStream(Stream stream, string contentType = null, long? contentLength = null, bool leaveOpen = false)
            => new Body<Stream>(stream ?? throw new ArgumentNullException(nameof(stream)), contentType, contentLength)
            {
                _ownsDisposable = !leaveOpen
            };

        /// <summary>
        /// Creates a new instance of <see cref="Body"/> with an object.
        /// </summary>
        /// <typeparam name="TContent">The type of the body <paramref name="value"/>.</typeparam>
        /// <param name="value">The object representing the body.</param>
        /// <param name="encoding"></param>
        /// <returns>The newly created <see cref="Body"/>.</returns>
        public static Body FromObject<TContent>(TContent value, Encoding encoding = null)
            => new Body<TContent>(value, encoding);

        internal static Body FromRequest(HttpRequest request)
            => new Body<Stream>(request.Body, request.ContentType, request.ContentLength)
            {
                _ownsDisposable = false,
                _settings = request.HttpContext.GetApiSimulatorSettings()
            };

        /// <summary>
        /// Deserializes object from request <see cref="Body"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="context">The <see cref="HttpContext"/>.</param> 
        /// <param name="settings">The <see cref="ApiSimulatorSettings"/>.</param> 
        /// <returns>A task that represents the asynchronous read operation.</returns>
        protected abstract Task<T> ReadAsync<T>(HttpContext context, ApiSimulatorSettings settings);

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
        /// Reads the body as a <see cref="Stream"/>.
        /// </summary>
        /// <returns>A <see cref="Stream"/> representing the body.</returns>
        public Stream ReadAsStream()
            => Content is Stream content
                ? content
                : ReadContentAsStream(new DefaultHttpContext(), _settings);

        /// <summary>
        /// Reads the body as a <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> representing the body.</returns>
        public string ReadAsString()
            => Content is string content
                ? content
                : ReadContentAsString(new DefaultHttpContext(), _settings);

        /// <summary>
        /// Deserializes the body into a <typeparamref name="TObject"/>.
        /// </summary>
        /// <typeparam name="TObject">Type of the object to deserialize to.</typeparam>
        /// <returns>The deserialized object.</returns>
        public TObject ReadAs<TObject>()
            => Content is TObject content
                ? content
                : ReadContentAs<TObject>(new DefaultHttpContext(), _settings);

        /// <summary>
        /// Returns a string that represents the current <see cref="Body"/>.
        /// </summary>
        /// <returns>A string that represents the current <see cref="Body"/>.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (ContentType != null)
                sb.AppendFormat(CultureInfo.InvariantCulture, "Content-Type: {0}\n", ContentType);
            if (ContentLength.HasValue)
                sb.AppendFormat(CultureInfo.InvariantCulture, "Content-Length: {0}\n", ContentLength);
            if (sb.Length > 0 && Content != null)
                sb.Append('\n');
            if (Content is Stream stream)
                sb.Append(stream.ToString());
            else if (Content != null)
                sb.Append(ReadContentAsString(new DefaultHttpContext(), _settings));
            return sb.ToString();
        }

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
                (Content as IDisposable)?.Dispose();

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
        {
            var stream = new InternalStringStream(httpBody, encoding);
            return new Body<Stream>(stream, mediaType + "; charset=" + encoding.HeaderName, stream.Length)
            {
                _ownsDisposable = true
            };
        }

        private MemoryStream ReadContentAsStream(HttpContext context, ApiSimulatorSettings settings)
        {
            var body = new MemoryStream();

            context.Response.Body = body;

            WriteAsync(context, settings ?? new ApiSimulatorSettings())
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            body.Position = 0;

            return body;
        }

        private string ReadContentAsString(HttpContext context, ApiSimulatorSettings settings)
        {
            using (MemoryStream body = ReadContentAsStream(context, settings))
            {
                Encoding encoding = context.GetApiSimulatorBodyEncoding() ?? Encoding.UTF8;
                return encoding.GetString(body.ToArray());
            }
        }

        private TObject ReadContentAs<TObject>(DefaultHttpContext context, ApiSimulatorSettings settings)
        {
            settings = settings ?? new ApiSimulatorSettings();
            Stream body = Content is Stream content
                ? content
                : ReadContentAsStream(context, settings);

            context.Request.Body = body;
            context.Request.ContentType = Content is Stream ? ContentType : (ContentType ?? context.Response.ContentType);
            context.Request.ContentLength = Content is Stream ? ContentLength : (ContentLength ?? context.Response.ContentLength);

            return ReadAsync<TObject>(context, settings)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        internal static async Task CopyBytesAsync(Stream input, Stream output, long? length)
        {
            var bytesAvailable = length;
            var bufferSize = 81920;

            if (bytesAvailable.HasValue && bytesAvailable.Value < bufferSize)
                bufferSize = (int)bytesAvailable.Value;

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int bytesRead;
                while (bufferSize > 0 && (bytesRead = await input.ReadAsync(buffer, 0, bufferSize).ConfigureAwait(false)) != 0)
                {
                    await output
                        .WriteAsync(buffer, 0, bytesRead)
                        .ConfigureAwait(false);
                    if (bytesAvailable.HasValue)
                    {
                        bytesAvailable = bytesAvailable.Value - bytesRead;
                        if (bytesAvailable.Value < bufferSize)
                            bufferSize = (int)bytesAvailable.Value;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        private class InternalStringStream : MemoryStream
        {
            private readonly Encoding _encoding;

            public InternalStringStream(string value, Encoding encoding)
                : base(encoding.GetBytes(value))
            {
                _encoding = encoding;
            }

            public override string ToString()
                => _encoding.GetString(ToArray());
        }
    }
}
