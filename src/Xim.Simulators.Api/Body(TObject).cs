using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Xim.Simulators.Api.Internal;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Represents a generic HTTP response body.
    /// </summary>
    /// <typeparam name="TObject">The type of the body content.</typeparam>
    public sealed class Body<TObject> : Body
    {
        private const string JsonEncoding = "application/json";
        private const string XmlEncoding = "application/xml";
        private const string BinaryEncoding = "application/octet-stream";

        private readonly Encoding _encoding;

        /// <summary>
        /// Gets the body content.
        /// </summary>
        public new TObject Content => (TObject)base.Content;

        internal Body(TObject content, string contentType, long? contentLength)
            : base(content, contentType, contentLength) { }

        internal Body(TObject content, Encoding encoding)
            : base(content, null, null)
        {
            _encoding = encoding;
        }

        /// <summary>
        /// Deserializes object from request <see cref="Body"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="context">The <see cref="HttpContext"/>.</param> 
        /// <param name="settings">The <see cref="ApiSimulatorSettings"/>.</param> 
        /// <returns>A task that represents the asynchronous read operation.</returns>
        protected override Task<T> ReadAsync<T>(HttpContext context, ApiSimulatorSettings settings)
            => ReadStreamAsync<T>(context.Request.Body, context.Request, settings);

        /// <summary> 
        /// Writes the body to the response stream. 
        /// </summary> 
        /// <param name="context">The <see cref="HttpContext"/>.</param> 
        /// <param name="settings">The <see cref="ApiSimulatorSettings"/>.</param> 
        /// <returns>A task that represents the asynchronous write operation.</returns>
        protected override Task WriteAsync(HttpContext context, ApiSimulatorSettings settings)
            => Content is Stream stream
                ? WriteStreamAsync(stream, context.Response)
                : WriteContentAsync(context, settings);

        private Task WriteStreamAsync(Stream stream, HttpResponse response)
        {
            response.ContentType = ContentType ?? BinaryEncoding;
            response.ContentLength = ContentLength ?? response.ContentLength;

            return CopyBytesAsync(stream, response.Body, response.ContentLength);
        }

        private Task WriteContentAsync(HttpContext context, ApiSimulatorSettings settings)
        {
            var accepts = ((string)context.Request.Headers["Accept"] ?? "")
                .Split(',')
                .Where(accept => !string.IsNullOrWhiteSpace(accept))
                .Select(accept => accept.Split(';')[0].Trim())
                .Distinct()
                .ToLookup(key => key, _ => true, StringComparer.InvariantCultureIgnoreCase);

            var acceptsAny = accepts["*/*"].Any();
            var acceptsJson = acceptsAny || accepts[JsonEncoding].Any();
            var acceptsXml = acceptsAny || accepts[XmlEncoding].Any();

            byte[] data;
            string contentType;
            Encoding charset;

            if (settings.XmlSettings != null && ((acceptsXml && !acceptsJson) || settings.JsonSettings == null))
            {
                var xmlSettings = settings.XmlSettings;
                contentType = XmlEncoding;
                charset = _encoding ?? xmlSettings.Encoding ?? Encoding.UTF8;
                data = SerializeXml(Content, xmlSettings, charset);
            }
            else if (settings.JsonSettings != null)
            {
                var jsonSettings = settings.JsonSettings;
                contentType = JsonEncoding;
                charset = _encoding ?? Encoding.UTF8;
                data = SerializeJson(Content, jsonSettings, charset);
            }
            else
            {
                throw new FormatException(SR.Format(SR.ApiResponseNotFormatted));
            }

            context.SetApiSimulatorBodyEncoding(charset);

            context.Response.ContentType = contentType + "; charset=" + charset.HeaderName;
            context.Response.ContentLength = data.Length;

            return context.Response.Body.WriteAsync(data, 0, data.Length);
        }

        private static byte[] SerializeJson<T>(T value, JsonSerializerOptions options, Encoding encoding)
            => encoding == null || encoding == Encoding.UTF8
                ? JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), options)
                : encoding.GetBytes(JsonSerializer.Serialize(value, typeof(T), options));

        private static byte[] SerializeXml<T>(T value, XmlWriterSettings xmlSettings, Encoding encoding)
        {
            using (var ms = new MemoryStream())
            using (var sr = new StreamWriter(ms, encoding))
            using (var xmlWriter = XmlWriter.Create(sr, xmlSettings))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                xmlSerializer.Serialize(xmlWriter, value, namespaces);
                return ms.ToArray();
            }
        }

        private static async Task CopyBytesAsync(Stream input, Stream output, long? length)
        {
            var bytesAvailable = length;
            var bufferSize = 81920;

            if (bytesAvailable.HasValue && bytesAvailable.Value < bufferSize)
                bufferSize = (int)bytesAvailable.Value;

            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
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

        private static async Task<T> ReadStreamAsync<T>(Stream body, HttpRequest request, ApiSimulatorSettings settings)
        {
            if (request.ContentType?.Contains(JsonEncoding) == true)
            {
                using (var ms = new MemoryStream())
                {
                    await CopyBytesAsync(body, ms, request.ContentLength).ConfigureAwait(false);
                    var charset = request.HttpContext.GetApiSimulatorBodyEncoding() ?? request.GetCharset();
                    return await DeserializeJsonAsync<T>(ms, settings.JsonSettings, charset).ConfigureAwait(false);
                }
            }
            else if (request.ContentType?.Contains(XmlEncoding) == true)
            {
                using (var ms = new MemoryStream())
                {
                    await CopyBytesAsync(body, ms, request.ContentLength).ConfigureAwait(false);
                    ms.Position = 0;
                    var charset = request.HttpContext.GetApiSimulatorBodyEncoding() ?? request.GetCharset() ?? Encoding.UTF8;
                    return DeserializeXml<T>(ms, new XmlReaderSettings(), charset);
                }
            }
            else
                throw new NotSupportedException(SR.Format(SR.ApiRequestFormatNotSupported, request.ContentType));
        }

        private static ValueTask<T> DeserializeJsonAsync<T>(Stream data, JsonSerializerOptions options, Encoding encoding)
        {
            if (encoding == null || encoding == Encoding.UTF8)
            {
                return JsonSerializer
                    .DeserializeAsync<T>(data, options);
            }
            else
            {
                using (var reader = new StreamReader(data, encoding))
                {
                    string json = reader.ReadToEnd();
                    T result = JsonSerializer.Deserialize<T>(json, options);
                    return new ValueTask<T>(result);
                }
            }
        }

        private static T DeserializeXml<T>(Stream data, XmlReaderSettings xmlSettings, Encoding encoding)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var sr = new StreamReader(data, encoding ?? Encoding.UTF8))
            using (var xmlReader = XmlReader.Create(sr, xmlSettings))
            {
                return (T)xmlSerializer.Deserialize(xmlReader);
            }
        }
    }
}
