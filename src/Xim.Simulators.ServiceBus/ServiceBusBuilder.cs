using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Xim.Simulators.ServiceBus
{
    /// <summary>
    /// Builds a service bus simulator.
    /// </summary>
    public sealed class ServiceBusBuilder
    {
        private readonly ISimulation _simulation;
        private readonly List<Topic> _topics;
        private readonly List<Queue> _queues;
        private readonly Dictionary<string, bool> _entities = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the <see cref="ILoggerProvider"/>. The default value is null.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SetLoggerProvider(ILoggerProvider)"/> to override the default value.
        /// </remarks>
        public ILoggerProvider LoggerProvider { get; private set; }

        /// <summary>
        /// Gets the <see cref="X509Certificate2"/> the service bus will use to secure the connection.
        /// The default vaue is null.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SetCertificate(X509Certificate2)"/> to override the default value.
        /// If the certificate is not set, the service bust will run unencrypted.
        /// </remarks>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// Gets the port the service bus will listen on. The default value is 0.
        /// </summary>
        /// <remarks>
        /// Use <see cref="SetPort(int)"/> to override the default value. If the default value 0
        /// is used, service bus will try to listen on port 5671 if secured by certificate, or 5672
        /// if not secured. If the default ports are not available a random port will be used.
        /// </remarks>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the collection of topics to register with the service bus.
        /// </summary>
        public IReadOnlyList<Topic> Topics { get; }

        /// <summary>
        /// Gets the collection of queues to register with the service bus.
        /// </summary>
        public IReadOnlyList<Queue> Queues { get; }

        internal ServiceBusBuilder(ISimulation simulation)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _topics = new List<Topic>();
            _queues = new List<Queue>();
            Topics = _topics.AsReadOnly();
            Queues = _queues.AsReadOnly();
        }

        /// <summary>
        /// Sets the <see cref="ILoggerProvider"/>.
        /// </summary>
        /// <param name="loggerProvider">The logger provider or null.</param>
        /// <returns>The <see cref="ServiceBusBuilder"/>.</returns>
        public ServiceBusBuilder SetLoggerProvider(ILoggerProvider loggerProvider)
        {
            LoggerProvider = loggerProvider;
            return this;
        }

        /// <summary>
        /// Sets the port the servie bus will listen on.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <remarks>If 0 is used, service bus will listen on the default port or any available
        /// port. See <see cref="Port"/> for more details.</remarks>
        /// <returns>The <see cref="ServiceBusBuilder"/>.</returns>
        public ServiceBusBuilder SetPort(int port)
        {
            Port = port;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="X509Certificate2"/>.
        /// </summary>
        /// <param name="certificate">The certificate to set or null.</param>
        /// <returns>The <see cref="ServiceBusBuilder"/>.</returns>
        public ServiceBusBuilder SetCertificate(X509Certificate2 certificate)
        {
            Certificate = certificate;
            return this;
        }

        /// <summary>
        /// Adds a topic to the builder.
        /// </summary>
        /// <param name="topic">The <see cref="Topic"/> to add.</param>
        /// <returns>The <see cref="ServiceBusBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="topic"/> is null.</exception>
        /// <exception cref="ArgumentException">If another entity with the same name was previously added.</exception>
        public ServiceBusBuilder AddTopic(Topic topic)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));
            if (_entities.ContainsKey(topic.Name))
                throw new ArgumentException(SR.Format(SR.SbEntityNameNotUnique, topic.Name), nameof(topic));
            Action addEntities = () => _entities[topic.Name] = true;
            foreach (Subscription subscription in topic.Subscriptions)
            {
                var subName = $"{topic.Name}/Subscriptions/{subscription.Name}";
                if (_entities.ContainsKey(subName))
                    throw new ArgumentException(SR.Format(SR.SbEntityNameNotUnique, subName), nameof(topic));
                addEntities += () => _entities[subName] = true;
            }
            _topics.Add(topic);
            addEntities.Invoke();
            return this;
        }

        /// <summary>
        /// Adds a queue to the builder.
        /// </summary>
        /// <param name="queue">The <see cref="Queue"/> to add.</param>
        /// <returns>The <see cref="ServiceBusBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="queue"/> is null.</exception>
        /// <exception cref="ArgumentException">If another entity with the same name was previously added.</exception>
        public ServiceBusBuilder AddQueue(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (_entities.ContainsKey(queue.Name))
                throw new ArgumentException(SR.Format(SR.SbEntityNameNotUnique, queue.Name), nameof(queue));
            _queues.Add(queue);
            _entities[queue.Name] = true;
            return this;
        }

        /// <summary>
        /// Builds the api simulator.
        /// </summary>
        /// <returns>New <see cref="ServiceBusSimulator"/> instance.</returns>
        /// <remarks>
        /// The newly created service bus simulator will be registered with the current simulation.
        /// </remarks>
        public IServiceBusSimulator Build()
            => ((IAddSimulator)_simulation).Add(new ServiceBusSimulator(this));
    }
}
