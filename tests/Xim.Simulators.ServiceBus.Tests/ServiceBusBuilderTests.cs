using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class ServiceBusBuilderTests
    {
        [Test]
        public void Constructor_Throws_WhenSimulationNull()
        {
            Action action = () => new ServiceBusBuilder(null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("simulation");
        }

        [Test]
        public void SetLoggerProvider_SetsLoggerProvider()
        {
            ILoggerProvider loggerProvider = Substitute.For<ILoggerProvider>();
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            ServiceBusBuilder self = serviceBusBuilder.SetLoggerProvider(loggerProvider);

            serviceBusBuilder.LoggerProvider.ShouldBeSameAs(loggerProvider);
            self.ShouldBe(serviceBusBuilder);
        }

        [Test]
        public void SetPort_SetsPort()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            ServiceBusBuilder self = serviceBusBuilder.SetPort(1237);

            serviceBusBuilder.Port.ShouldBe(1237);
            self.ShouldBe(serviceBusBuilder);
        }

        [Test]
        public void SetCertificate_SetsCertificate()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());
            var cert = new X509Certificate2();

            ServiceBusBuilder self = serviceBusBuilder.SetCertificate(cert);

            serviceBusBuilder.Certificate.ShouldBeSameAs(cert);
            self.ShouldBe(serviceBusBuilder);
        }

        [Test]
        public void AddTopic_Throws_WhenTopicNull()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            Should.Throw<ArgumentNullException>(() => serviceBusBuilder.AddTopic(null))
                .ParamName.ShouldBe("topic");
        }

        [Test]
        public void AddTopic_AddsTopic()
        {
            Subscription[] subscriptions = null;
            var topic = new Topic("a", subscriptions);
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            ServiceBusBuilder self = serviceBusBuilder.AddTopic(topic);

            serviceBusBuilder.ShouldSatisfyAllConditions(
                () => serviceBusBuilder.Topics.Count.ShouldBe(1),
                () => serviceBusBuilder.Topics[0].ShouldBeSameAs(topic),
                () => self.ShouldBeSameAs(serviceBusBuilder)
            );
        }

        [Test]
        public void AddTopic_Throws_WhenTopicWithTheSameNameCIAlreadyExists()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            serviceBusBuilder.AddTopic(new Topic("a"));
            serviceBusBuilder.AddTopic(new Topic("b"));

            ArgumentException exception = Should.Throw<ArgumentException>(() => serviceBusBuilder.AddTopic(new Topic("A")));
            exception.ParamName.ShouldBe("topic");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotUnique, "A"));
        }

        [Test]
        public void AddTopic_Throws_WhenQueueWithTheSameNameCIAlreadyExists()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            serviceBusBuilder.AddQueue(new Queue("z"));

            ArgumentException exception = Should.Throw<ArgumentException>(() => serviceBusBuilder.AddTopic(new Topic("Z")));

            exception.ParamName.ShouldBe("topic");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotUnique, "Z"));
        }

        [Test]
        public void AddTopic_Throws_WhenEntityWithTheSameNameCIAlreadyExists()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            serviceBusBuilder.AddQueue(new Queue("a/Subscriptions/b"));

            ArgumentException exception = Should.Throw<ArgumentException>(() => serviceBusBuilder.AddTopic(new Topic("A", new Subscription("b"))));
            exception.ParamName.ShouldBe("topic");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotUnique, "A/Subscriptions/b"));
        }

        [Test]
        public void AddQueue_Throws_WhenQueueNull()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            Should.Throw<ArgumentNullException>(() => serviceBusBuilder.AddQueue(null))
                .ParamName.ShouldBe("queue");
        }

        [Test]
        public void AddQueue_AddsQueue()
        {
            var queue = new Queue("a");
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            ServiceBusBuilder self = serviceBusBuilder.AddQueue(queue);

            serviceBusBuilder.ShouldSatisfyAllConditions(
                () => serviceBusBuilder.Queues.Count.ShouldBe(1),
                () => serviceBusBuilder.Queues[0].ShouldBeSameAs(queue),
                () => self.ShouldBeSameAs(serviceBusBuilder)
            );
        }

        [Test]
        public void AddQueue_Throws_WhenQueueWithTheSameNameCIAlreadyExists()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            serviceBusBuilder.AddQueue(new Queue("x"));
            serviceBusBuilder.AddQueue(new Queue("y"));

            Should.Throw<ArgumentException>(() => serviceBusBuilder.AddQueue(new Queue("X")))
                .ParamName.ShouldBe("queue");
        }

        [Test]
        public void AddQueue_Throws_WhenTopicWithTheSameNameCIAlreadyExists()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            serviceBusBuilder.AddTopic(new Topic("x"));

            Should.Throw<ArgumentException>(() => serviceBusBuilder.AddQueue(new Queue("X")))
                .ParamName.ShouldBe("queue");
        }

        [Test]
        public void AddQueue_Throws_WhenEntityWithTheSameNameCIAlreadyExists()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            serviceBusBuilder.AddTopic(new Topic("x", new Subscription("y")));

            Should.Throw<ArgumentException>(() => serviceBusBuilder.AddQueue(new Queue("X/subscriptions/Y")))
                .ParamName.ShouldBe("queue");
        }

        [Test]
        public void Build_AddsSimulatorToSimulation()
        {
            ISimulation simulation = Substitute.For<ISimulation, IAddSimulator>();
            var serviceBusBuilder = new ServiceBusBuilder(simulation);

            ((IAddSimulator)simulation).Add(Arg.Any<ServiceBusSimulator>())
                .Returns(info => info.ArgAt<ServiceBusSimulator>(0));

            var serviceBusSimulator = (ServiceBusSimulator)serviceBusBuilder.Build();

            ((IAddSimulator)simulation).Received(1).Add(serviceBusSimulator);
        }
    }
}
