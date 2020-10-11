using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Api simulator settings.
    /// </summary>
    public class ApiSimulatorSettings
    {
        /// <summary>
        /// Gets the <see cref="ILoggerProvider"/> or null if none set.
        /// </summary>
        public ILoggerProvider LoggerProvider { get; }

        /// <summary>
        /// Returns the <see cref="X509Certificate2"/> used to setup HTTPS, or null if none set.
        /// </summary>
        /// <remarks>
        /// If no certificate is present, api server will run over plain HTTP protocol.
        /// </remarks>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Gets the collection of api handlers.
        /// </summary>
        public ApiHandlerCollection Handlers { get; }

        /// <summary>
        /// Gets the default <see cref="ApiHandler"/>.
        /// </summary>
        public ApiHandler DefaultHandler { get; }

        /// <summary>
        /// Gets the preferred api port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/>.
        /// </summary>
        public JsonSerializerOptions JsonSettings { get; }

        /// <summary>
        /// Gets the <see cref="XmlWriterSettings"/>.
        /// </summary>
        public XmlWriterSettings XmlSettings { get; }

        internal ApiSimulatorSettings()
        {
            JsonSettings = new JsonSerializerOptions { WriteIndented = true };
        }

        internal ApiSimulatorSettings(ApiBuilder builder)
        {
            LoggerProvider = builder.LoggerProvider;
            Certificate = builder.Certificate;
            Handlers = (ApiHandlerCollection)((ICloneable)builder.Handlers).Clone();
            DefaultHandler = builder.DefaultHandler;
            Port = builder.Port;
            JsonSettings = builder.JsonSettings;
            XmlSettings = builder.XmlSettings;
        }
    }
}
