using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Builds an api simulator.
    /// </summary>
    public sealed class ApiBuilder
    {
        private readonly ISimulation _simulation;

        /// <summary>
        /// Gets the <see cref="ILoggerProvider"/>. The default value is null.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SetLoggerProvider(ILoggerProvider)"/> to override the default value.
        /// </remarks>
        public ILoggerProvider LoggerProvider { get; private set; }

        /// <summary>
        /// Gets the <see cref="X509Certificate2"/> the api will use to secure the connection.
        /// The default vaue is null.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SetCertificate(X509Certificate2)"/> to override the default value.
        /// If the certificate is not set, the api will run unencrypted.
        /// </remarks>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// Gets the port the api will listen on. The default value is 0.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SetPort(int)"/> to override the default value. If the default value 0
        /// is used, api will listen on any available port.
        /// </remarks>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the <see cref="ApiHandlerCollection"/> of handlers to register with the api.
        /// </summary>
        /// <remarks>Use <see cref="AddHandler(string, ApiHandler)"/> to set handlers.</remarks>
        public ApiHandlerCollection Handlers { get; } = new ApiHandlerCollection();

        /// <summary>
        /// Gets the default <see cref="ApiHandler"/>.
        /// </summary>
        /// <remarks>
        /// This handler is used when the api cannot match incoming requests registered with the
        /// <see cref="Handlers"/> collection. The default implementation returns 404 Not Found.
        /// Use <see cref="SetDefaultHandler(ApiHandler)"/> to override the default implementation.
        /// </remarks>
        public ApiHandler DefaultHandler { get; private set; } = _ => Task.FromResult(new ApiResponse(404));

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> used to serialize response body.
        /// </summary>
        public JsonSerializerOptions JsonSettings { get; private set; } = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Gets the <see cref="XmlWriterSettings"/> used to serialize response body.
        /// </summary>
        public XmlWriterSettings XmlSettings { get; private set; } = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8
        };

        internal ApiBuilder(ISimulation simulation)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        }

        /// <summary>
        /// Sets the <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="loggerProvider">The logger provider or null.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        public ApiBuilder SetLoggerProvider(ILoggerProvider loggerProvider)
        {
            LoggerProvider = loggerProvider;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="X509Certificate2"/>.
        /// </summary>
        /// <param name="certificate">The certificate to set or null.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        public ApiBuilder SetCertificate(X509Certificate2 certificate)
        {
            Certificate = certificate;
            return this;
        }

        /// <summary>
        /// Sets the port the api will listen on.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <remarks>If 0 is used, api will listen on any available port.</remarks>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        public ApiBuilder SetPort(int port)
        {
            Port = port;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="JsonSerializerOptions"/>.
        /// </summary>
        /// <param name="jsonSettings">The JSON serializer settings or null.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <remarks>Set to null to disable JSON serialization.</remarks>
        public ApiBuilder SetJsonSettings(JsonSerializerOptions jsonSettings)
        {
            JsonSettings = jsonSettings;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="XmlWriterSettings"/>.
        /// </summary>
        /// <param name="xmlSettings">The XML serializer settongs or null.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <remarks>Set to null to disable XML serialization.</remarks>
        public ApiBuilder SetXmlSettings(XmlWriterSettings xmlSettings)
        {
            XmlSettings = xmlSettings;
            return this;
        }

        /// <summary>
        /// Registers an <see cref="ApiHandler"/> with an action.
        /// </summary>
        /// <param name="action">The action to register the handler with, i.e. <c>"GET /index"</c>.</param>
        /// <param name="handler">The handler.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="action"/> or <paramref name="handler"/> is null.</exception>
        public ApiBuilder AddHandler(string action, ApiHandler handler)
        {
            Handlers.Set(action, handler);
            return this;
        }

        /// <summary>
        /// Registers a templated <see cref="ApiHandler{TArg}"/>.
        /// </summary>
        /// <typeparam name="T">The type to bind the template to.</typeparam>
        /// <param name="action">The action template to register the handler with, i.e. <c>"GET /books/{id}"</c>.</param>
        /// <param name="handler">The handler.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <remarks>
        /// Use this method to set a templated handler:
        ///     <code>apiBuilder.AddHandler("GET /books/{id}", (id, ctx) => Task.FromResult(ApiResponse.Ok(GetBook(id, ctx))));</code>
        /// Only primitive types and value tuples are supported.
        /// </remarks>
        /// <exception cref="ArgumentNullException">If action or handler is null.</exception>
        /// <exception cref="ArgumentException">If action template is invalid or cannot be bound to the template type.</exception>
        public ApiBuilder AddHandler<T>(string action, ApiHandler<T> handler)
        {
            Handlers.Set(action, handler);
            return this;
        }

        /// <summary>
        /// Sets the default <see cref="ApiHandler"/>.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <remarks>
        /// If you set this to null, the api will return 502 Bad Gateway for requests that cannot
        /// be handled.
        /// </remarks>
        public ApiBuilder SetDefaultHandler(ApiHandler handler)
        {
            DefaultHandler = handler;
            return this;
        }

        /// <summary>
        /// Builds the api simulator.
        /// </summary>
        /// <returns>New <see cref="ApiSimulator"/> instance.</returns>
        /// <remarks>
        /// The newly created api simulator will be registered with the current simulation.
        /// </remarks>
        public IApiSimulator Build()
            => ((IAddSimulator)_simulation).Add(new ApiSimulator(this));
    }
}