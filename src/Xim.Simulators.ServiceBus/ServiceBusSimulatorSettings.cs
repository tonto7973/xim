using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Xim.Simulators.ServiceBus
{
    /// <summary>
    /// Service bus simulator settings.
    /// </summary>
    public class ServiceBusSimulatorSettings
    {
        /// <summary>
        /// Gets the <see cref="ILoggerProvider"/> or null if none set.
        /// </summary>
        public ILoggerProvider LoggerProvider { get; }

        /// <summary>
        /// Returns the <see cref="X509Certificate2"/> used to setup secure links, or null if none set.
        /// </summary>
        /// <remarks>
        /// If no certificate is present, service bus host will run over plain text protocol.
        /// </remarks>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Gets the preferred service bus port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the list of registered topics with subscriptions.
        /// </summary>
        public IReadOnlyList<Topic> Topics { get; }

        /// <summary>
        /// Gets the list of registered queues.
        /// </summary>
        public IReadOnlyList<Queue> Queues { get; }

        internal ServiceBusSimulatorSettings(ServiceBusBuilder builder)
        {
            LoggerProvider = builder.LoggerProvider ?? NullLoggerProvider.Instance;
            Certificate = builder.Certificate;
            Port = builder.Port;
            Topics = builder.Topics;
            Queues = builder.Queues;
        }
    }
}
