using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class ServiceBusSimulatorSettingsTests
    {
        [Test]
        public void Constructor_SetsLoggerProvider()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var loggerProvider = Substitute.For<ILoggerProvider>();
            serviceBusBuilder.SetLoggerProvider(loggerProvider);
            var settings = new ServiceBusSimulatorSettings(serviceBusBuilder);
            settings.LoggerProvider.ShouldBeSameAs(loggerProvider);
        }

        [Test]
        public void Constructor_SetsCertificate()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var certificate = new X509Certificate2();
            serviceBusBuilder.SetCertificate(certificate);
            var settings = new ServiceBusSimulatorSettings(serviceBusBuilder);
            settings.Certificate.ShouldBeSameAs(certificate);
        }

        [Test]
        public void Constructor_SetsPort()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            serviceBusBuilder.SetPort(7269);
            var settings = new ServiceBusSimulatorSettings(serviceBusBuilder);
            settings.Port.ShouldBe(7269);
        }

        [Test]
        public void Constructor_SetsTopics()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            serviceBusBuilder.AddTopic(new Topic("new"));
            var settings = new ServiceBusSimulatorSettings(serviceBusBuilder);
            settings.Topics.ShouldBeSameAs(serviceBusBuilder.Topics);
        }

        [Test]
        public void Constructor_SetsQueues()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            serviceBusBuilder.AddTopic(new Topic("new"));
            var settings = new ServiceBusSimulatorSettings(serviceBusBuilder);
            settings.Queues.ShouldBeSameAs(serviceBusBuilder.Queues);
        }
    }
}
