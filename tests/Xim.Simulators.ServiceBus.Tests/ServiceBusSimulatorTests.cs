using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class ServiceBusSimulatorTests
    {
        [Test]
        public void Constructor_SetsSettings()
        {
            const int port = 8794;
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var certificate = new X509Certificate2();
            serviceBusBuilder.SetLoggerProvider(loggerProvider);
            serviceBusBuilder.SetCertificate(certificate);
            serviceBusBuilder.SetPort(port);

            var simulator = new ServiceBusSimulator(serviceBusBuilder);

            simulator.Settings.LoggerProvider.ShouldBeSameAs(loggerProvider);
            simulator.Settings.Certificate.ShouldBeSameAs(certificate);
            simulator.Settings.Port.ShouldBe(port);
        }

        [Test]
        public void Constructor_SetsTopicsCI()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            serviceBusBuilder.AddTopic(new Topic("xyz", new Subscription("a"), new Subscription("b")));
            serviceBusBuilder.AddTopic(new Topic("V"));

            var simulator = new ServiceBusSimulator(serviceBusBuilder);

            simulator.ShouldSatisfyAllConditions(
                () => simulator.Topics.Count.ShouldBe(2),
                () => simulator.Topics["XYZ"].Subscriptions.Count.ShouldBe(2),
                () => simulator.Topics["xyz"].Subscriptions["A"].Name.ShouldBe("a"),
                () => simulator.Topics["xYz"].Subscriptions["b"].Name.ShouldBe("b"),
                () => simulator.Topics["v"].Subscriptions.ShouldBeEmpty(),
                () => simulator.Topics["v"].Name.ShouldBe("V")
            );
        }

        [Test]
        public void Constructor_SetsQueuesCI()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            serviceBusBuilder.AddQueue(new Queue("One"));
            serviceBusBuilder.AddQueue(new Queue("two"));

            var simulator = new ServiceBusSimulator(serviceBusBuilder);

            simulator.ShouldSatisfyAllConditions(
                () => simulator.Queues.Count.ShouldBe(2),
                () => simulator.Queues["one"].Name.ShouldBe("One"),
                () => simulator.Queues["tWO"].Name.ShouldBe("two")
            );
        }

        [Test]
        public void Dispose_DoesNotThrow_WhenCertificateNotSet()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            Action action = () => serviceBusSimulator.Dispose();

            action.ShouldNotThrow();
        }

        [Test]
        public void Dispose_DoesNotThrow_WhenCalledTwice()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            Action action = () =>
            {
                serviceBusSimulator.Dispose();
                serviceBusSimulator.Dispose();
            };

            action.ShouldNotThrow();
        }

        [Test]
        public void Dispose_DisposesTopics()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>())
                .AddTopic("myTopic");
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);
            var topic = serviceBusSimulator.Topics["myTopic"];

            serviceBusSimulator.Dispose();

            Should.Throw<ObjectDisposedException>(() => topic.Post(new Amqp.Message()));
        }

        [Test]
        public void Dispose_DisposesQueues()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>())
                .AddQueue("myQueue");
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);
            var queue = serviceBusSimulator.Queues["myQueue"];

            serviceBusSimulator.Dispose();

            Should.Throw<ObjectDisposedException>(() => queue.Post(new Amqp.Message()));
        }

        [Test]
        public async Task StartAsync_SetsStateToRunning()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            await serviceBusSimulator.StartAsync();
            try
            {
                serviceBusSimulator.State.ShouldBe(SimulatorState.Running);
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StopAsync_SetsStateToStopped()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            await serviceBusSimulator.StartAsync();
            try
            {
                await serviceBusSimulator.StopAsync();
                serviceBusSimulator.State.ShouldBe(SimulatorState.Stopped);
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartAsync_ReportsActivePort()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            await serviceBusSimulator.StartAsync();
            try
            {
                serviceBusSimulator.Port.ShouldNotBe(0);
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [TestCase(5678, true)]
        [TestCase(5677, false)]
        public async Task StartAsync_ReportsActivePortFromSettings_WhenSettingsPortNotZero(int port, bool secured)
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            serviceBusBuilder.SetPort(port);
            serviceBusBuilder.SetCertificate(secured ? new X509Certificate2() : null);
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            await serviceBusSimulator.StartAsync();
            try
            {
                serviceBusSimulator.Port.ShouldBe(port);
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartAsync_ReportsActiveLocation()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            await serviceBusSimulator.StartAsync();
            try
            {
                serviceBusSimulator.Location.ShouldStartWith($"amqp://127.0.0.1:{serviceBusSimulator.Port}");
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartAsync_ReportsActiveLocation_WhenSecured()
        {
            using (var certificate = TestCertificate.Create())
            {
                var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>())
                    .SetCertificate(certificate);
                var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

                await serviceBusSimulator.StartAsync();
                try
                {
                    serviceBusSimulator.Location.ShouldStartWith($"amqps://127.0.0.1:{serviceBusSimulator.Port}");
                }
                finally
                {
                    await serviceBusSimulator.StopAsync();
                }
            }
        }

        [Test]
        public async Task StartAsync_ReportsActiveConnectionString()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            await serviceBusSimulator.StartAsync();
            try
            {
                serviceBusSimulator.ConnectionString.ShouldBe($"Endpoint=sb://127.0.0.1:{serviceBusSimulator.Port};SharedAccessKeyName=all;SharedAccessKey=CLwo3FQ3S39Z4pFOQDefaiUd1dSsli4XOAj3Y9Uh1E=;TransportType=Amqp");
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartAsync_UsesDefaultPort_WhenNotSecured()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            await serviceBusSimulator.StartAsync();
            try
            {
                serviceBusSimulator.Port.ShouldBe(5672);
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartAsync_UsesRandomPort_WhenDefaultIsUsed()
        {
            using (TestTcpUtils.BlockAllLocalPorts(5672))
            {
                var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
                var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

                await serviceBusSimulator.StartAsync();
                try
                {
                    serviceBusSimulator.Port.ShouldNotBe(5672);
                }
                finally
                {
                    await serviceBusSimulator.StopAsync();
                }
            }
        }

        [Test]
        public async Task StartAsync_UsesDefaultSecurePort_WhenSecured()
        {
            using (var certificate = TestCertificate.Create())
            {
                var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>())
                    .SetCertificate(certificate);
                var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

                await serviceBusSimulator.StartAsync();
                try
                {
                    serviceBusSimulator.Port.ShouldBe(5671);
                }
                finally
                {
                    await serviceBusSimulator.StopAsync();
                }
            }
        }

        [Test]
        public async Task StartAsync_Throws_WhenPortAlreadyInUse()
        {
            var availablePort = TestTcpUtils.FindFreePort();
            using (TestTcpUtils.BlockAllLocalPorts(availablePort))
            {
                var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>()).SetPort(availablePort);
                var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

                Func<Task> action = async () => await serviceBusSimulator.StartAsync();
                try
                {
                    await action.ShouldThrowAsync<SocketException>();
                    serviceBusSimulator.State.ShouldBe(SimulatorState.Stopped);
                }
                finally
                {
                    await serviceBusSimulator.StopAsync();
                }
            }
        }

        [Test]
        public async Task StartAsync_Throws_WhenPortInvalid()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>()).SetPort(ushort.MaxValue + 10);
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            Func<Task> action = async () => await serviceBusSimulator.StartAsync();
            try
            {
                await action.ShouldThrowAsync<UriFormatException>();
                serviceBusSimulator.State.ShouldBe(SimulatorState.Stopped);
            }
            finally
            {
                await serviceBusSimulator.StopAsync();
            }
        }

        [Test]
        public void Port_Throws_WhenSimulatorNotRunning()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            Action action = () => serviceBusSimulator.Port.ShouldBe(0);

            action.ShouldThrow<InvalidOperationException>()
                .Message.ShouldBe(SR.Format(SR.SimulatorPropertyInvalid, "Port"));
        }

        [Test]
        public void Location_Throws_WhenSimulatorNotRunning()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            Action action = () => serviceBusSimulator.Location.ShouldBeNull();

            action.ShouldThrow<InvalidOperationException>()
                .Message.ShouldBe(SR.Format(SR.SimulatorPropertyInvalid, "Location"));
        }

        [Test]
        public void ConnectionString_Throws_WhenSimulatorNotRunning()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var serviceBusSimulator = new ServiceBusSimulator(serviceBusBuilder);

            Action action = () => serviceBusSimulator.ConnectionString.ShouldBeNull();

            action.ShouldThrow<InvalidOperationException>()
                .Message.ShouldBe(SR.Format(SR.SimulatorPropertyInvalid, "ConnectionString"));
        }
    }
}
