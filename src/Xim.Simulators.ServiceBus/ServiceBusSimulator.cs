using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using Amqp;
using Amqp.Listener;
using Microsoft.Extensions.Logging;
using Xim.Simulators.ServiceBus.Azure;
using Xim.Simulators.ServiceBus.Entities;
using Xim.Simulators.ServiceBus.Processing;
using Xim.Simulators.ServiceBus.Security;

namespace Xim.Simulators.ServiceBus
{
    internal sealed class ServiceBusSimulator : Simulator, IServiceBusSimulator
    {
        private const int DefaultSecurePort = 5671;
        private const int DefaultPort = 5672;

        private bool _disposed;
        private ContainerHost _containerHost;

        private readonly ISecurityContext _securityContext;
        private readonly CbsTokenValidator _cbsTokenValidator;
        private readonly EntityLookup _entityLookup;
        private readonly ILogger _logger;

        public ServiceBusSimulatorSettings Settings { get; }

        public int Port => GetPort();

        public string Location => GetLocation();

        public string ConnectionString => GetConnectionString();

        public IReadOnlyDictionary<string, ITopic> Topics { get; }

        public IReadOnlyDictionary<string, IQueue> Queues { get; }

        internal ServiceBusSimulator(ServiceBusBuilder builder)
        {
            Settings = new ServiceBusSimulatorSettings(builder);
            Topics = builder.Topics
                .Select(topic => (ITopic)new TopicEntity(topic.Name, topic.Subscriptions.Select(subscription => subscription.Name)))
                .ToDictionary(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
                .AsReadOnly();
            Queues = builder.Queues
                .Select(queue => (IQueue)new QueueEntity(queue.Name))
                .ToDictionary(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
                .AsReadOnly();
            _securityContext = SecurityContext.Default;
            _cbsTokenValidator = CbsTokenValidator.Default;
            _entityLookup = new EntityLookup(this);
            _logger = Settings.LoggerProvider.CreateLogger(nameof(ServiceBusSimulator));
        }

        private int GetPort()
            => TryGetLocation(out var location)
                ? new Uri(location).Port
                : throw new InvalidOperationException(SR.Format(SR.SimulatorPropertyInvalid, nameof(Port)));

        private string GetLocation()
            => TryGetLocation(out var location)
                ? location
                : throw new InvalidOperationException(SR.Format(SR.SimulatorPropertyInvalid, nameof(Location)));

        private string GetConnectionString()
                    => TryGetLocation(out var location)
                        ? $"Endpoint=sb://127.0.0.1:{new Uri(location).Port};SharedAccessKeyName={_cbsTokenValidator.SharedAccessKeyName};SharedAccessKey={_cbsTokenValidator.SharedAccessKey};TransportType=Amqp"
                        : throw new InvalidOperationException(SR.Format(SR.SimulatorPropertyInvalid, nameof(ConnectionString)));

        private bool TryGetLocation(out string location)
        {
            location = State == SimulatorState.Running
                ? ToLocation(_containerHost.Listeners[0].Address)
                : null;

            return location != null;
        }

        private static string ToLocation(Address address)
            => $"{address.Scheme}://127.0.0.1:{address.Port}";

        public override async Task StartAsync()
        {
            if (TrySetState(SimulatorState.Starting))
            {
                try
                {
                    _containerHost = Settings.Certificate != null
                        ? BuildSecureServiceBusHost()
                        : BuildServiceBusHost();
                    await StartContainerHostAsync(_containerHost).ConfigureAwait(false);
                }
                catch
                {
                    _containerHost?.Close();
                    _containerHost = null;
                    SetState(SimulatorState.Stopped);
                    throw;
                }

                SetState(SimulatorState.Running);
            }
        }

        public override Task StopAsync()
            => Task.Run(Abort);

        public override void Abort()
        {
            if (TrySetState(SimulatorState.Stopping))
            {
                try
                {
                    StopHost();
                }
                finally
                {
                    _containerHost = null;
                    SetState(SimulatorState.Stopped);
                }
            }
        }

        private void StopHost()
        {
            TryGetLocation(out var location);
            _containerHost.Close();
            _logger.LogDebug($"Host {location} stopped.");
        }

        private Task StartContainerHostAsync(IContainerHost host)
            => Task.Run(() =>
               {
                   host.RegisterRequestProcessor("$cbs", new CbsRequestProcessor(_securityContext, Settings.LoggerProvider, _cbsTokenValidator));
                   host.RegisterLinkProcessor(new LinkProcessor(_securityContext, _entityLookup, Settings.LoggerProvider));
                   host.RegisterManagementProcessors(_entityLookup, Settings.LoggerProvider);
                   host.Open();
                   TryGetLocation(out var location);
                   _logger.LogDebug($"Host {location} started.");
               });

        private ContainerHost BuildServiceBusHost()
        {
            var port = Settings.Port == 0 ? FindAvailablePort(DefaultPort) : Settings.Port;
            var address = new Address($"amqp://localhost:{port}");
            var host = new ContainerHost(address);

            host.Listeners[0].HandlerFactory = _ => AzureHandler.Instance;
            host.Listeners[0].SASL.EnableAzureSaslMechanism();
            host.Listeners[0].SASL.EnableExternalMechanism = true;
            host.Listeners[0].SASL.EnableAnonymousMechanism = true;

            _logger.LogDebug($"Starting service bus host at {address}.");

            return host;
        }

        private ContainerHost BuildSecureServiceBusHost()
        {
            var port = Settings.Port == 0 ? FindAvailablePort(DefaultSecurePort) : Settings.Port;
            var address = new Address($"amqps://localhost:{port}");
            var host = new ContainerHost(new[] { address }, Settings.Certificate);

            host.Listeners[0].HandlerFactory = _ => AzureHandler.Instance;
            host.Listeners[0].SASL.EnableAzureSaslMechanism();
            host.Listeners[0].SASL.EnableExternalMechanism = true;
            host.Listeners[0].SASL.EnableAnonymousMechanism = true;
            host.Listeners[0].SSL.ClientCertificateRequired = true;
            host.Listeners[0].SSL.RemoteCertificateValidationCallback = (_, __, ___, errors) =>
            {
                _logger.LogWarning($"AMQP SSL errors {errors}.");
                return errors == SslPolicyErrors.RemoteCertificateNotAvailable;
            };

            _logger.LogDebug($"Starting secure service bus host at {address}.");

            return host;
        }

        private static int FindAvailablePort(int preferred)
        {
            var listener = new TcpListener(IPAddress.Loopback, preferred);
            try
            {
                listener.Start();
            }
            catch (SocketException)
            {
                return NextAvailablePort();
            }

            listener.Stop();

            return preferred;
        }

        private static int NextAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            Abort();

            foreach (var topic in Topics.Values.OfType<TopicEntity>())
            {
                topic.Dispose();
            }

            foreach (var queue in Queues.Values.OfType<QueueEntity>())
            {
                queue.Dispose();
            }
        }
    }
}
