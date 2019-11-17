using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;

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

        internal Body(TObject content, string contentType) : base(content, contentType) { }

        internal Body(TObject content, Encoding encoding) : base(content, null)
        {
            _encoding = encoding;
        }

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
            return stream.CopyToAsync(response.Body);
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
    }
}
