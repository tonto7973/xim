using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class ServiceBusBuilderExtensionsTests
    {
        [Test]
        public void AddServiceBus_CreatesNewInstanceOfServiceBusSimulator()
        {
            var simulation = Substitute.For<ISimulation, IAddSimulator>();

            var serviceBusBuilder1 = simulation.AddServiceBus();
            var serviceBusBuilder2 = simulation.AddServiceBus();

            serviceBusBuilder1.Build();
            serviceBusBuilder2.Build();

            simulation.ShouldSatisfyAllConditions(
                () => serviceBusBuilder1.ShouldNotBeNull(),
                () => serviceBusBuilder2.ShouldNotBeNull(),
                () => serviceBusBuilder1.ShouldNotBeSameAs(serviceBusBuilder2),
                () => ((IAddSimulator)simulation).Received(2).Add(Arg.Any<ServiceBusSimulator>())
            );
        }

        [Test]
        public void AddTopic_Throws_WhenServiceBusBuilderNull()
        {
            ServiceBusBuilder serviceBusBuilder = null;

            Action action = () => serviceBusBuilder.AddTopic("foo", "bar");

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("serviceBusBuilder");
        }

        [Test]
        public void AddTopic_AddsTopicToBuilder()
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            var self = serviceBusBuilder.AddTopic("a");

            serviceBusBuilder.ShouldSatisfyAllConditions(
                () => serviceBusBuilder.Topics[0].Name.ShouldBe("a"),
                () => self.ShouldBeSameAs(serviceBusBuilder)
            );
        }

        [TestCase("a", new string[] { "c", "0" })]
        [TestCase("zxy", null)]
        public void AddTopic_AddsTopicWithSubscriptionsToBuilder(string topicName, string[] subs)
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            var self = serviceBusBuilder.AddTopic(topicName, subs);

            serviceBusBuilder.ShouldSatisfyAllConditions(
                () => serviceBusBuilder.Topics[0].Name.ShouldBe(topicName),
                () => serviceBusBuilder.Topics[0].Subscriptions.Count.ShouldBe(subs?.Length ?? 0),
                () => serviceBusBuilder.Topics[0].Subscriptions.Select(s => s.Name).ToArray().ShouldBe(subs ?? new string[0]),
                () => self.ShouldBeSameAs(serviceBusBuilder)
            );
        }

        [Test]
        public void AddQueue_Throws_WhenServiceBusBuilderNull()
        {
            ServiceBusBuilder serviceBusBuilder = null;

            Action action = () => serviceBusBuilder.AddQueue("foo");

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("serviceBusBuilder");
        }

        [TestCase("abe")]
        [TestCase("zxv")]
        public void AddQueue_AddsQueueToBuilder(string queueName)
        {
            var serviceBusBuilder = new ServiceBusBuilder(Substitute.For<ISimulation>());

            var self = serviceBusBuilder.AddQueue(queueName);

            serviceBusBuilder.ShouldSatisfyAllConditions(
                () => serviceBusBuilder.Queues[0].Name.ShouldBe(queueName),
                () => self.ShouldBeSameAs(serviceBusBuilder)
            );
        }
    }
}
