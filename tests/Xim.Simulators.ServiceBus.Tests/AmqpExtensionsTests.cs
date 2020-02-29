using System;
using System.Collections.Generic;
using Amqp.Listener;
using Amqp.Types;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Delivering;
using Xim.Simulators.ServiceBus.Entities;
using Xim.Simulators.ServiceBus.Processing;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class AmqpExtensionsTests
    {
        [Test]
        public void Clone_ReturnsNullWhenMessageNull() => AmqpExtensions.Clone(null).ShouldBeNull();

        [Test]
        public void Clone_ReturnsCloneOfTheMessage_WhenMessageProvided()
        {
            var testExpiryTime = DateTime.UtcNow.Date;
            var message = new Amqp.Message("Abc")
            {
                Header = new Amqp.Framing.Header
                {
                    Durable = true
                },
                Properties = new Amqp.Framing.Properties
                {
                    CorrelationId = "myCorrelationId",
                    AbsoluteExpiryTime = testExpiryTime
                }
            };

            var clone = message.Clone();

            clone.ShouldSatisfyAllConditions(
                () => clone.ShouldNotBeSameAs(message),
                () => clone.Header.ShouldNotBeSameAs(message.Header),
                () => clone.Header.Durable.ShouldBeTrue(),
                () => clone.Properties.ShouldNotBeSameAs(message.Properties),
                () => clone.Properties.CorrelationId.ShouldBe("myCorrelationId"),
                () => clone.Properties.AbsoluteExpiryTime.ShouldBe(testExpiryTime)
            );
        }

        [Test]
        public void AddSequenceNumber_ReturnsNullWhenMessageNull() => AmqpExtensions.AddSequenceNumber(null, 123456L).ShouldBeNull();

        [TestCase(17L)]
        [TestCase(5487951246523L)]
        public void AddSequenceNumber_AddsSequenceNumber(long sequenceNo)
        {
            var message = new Amqp.Message();

            message.AddSequenceNumber(sequenceNo);

            message.ShouldSatisfyAllConditions(
                () => message.MessageAnnotations.ShouldNotBeNull(),
                () => message.MessageAnnotations[(Symbol)"x-opt-sequence-number"].ShouldBe(sequenceNo)
            );
        }

        [Test]
        public void RegisterManagementProcessors_DoesNotRegisterManagementProcessor_WhenEntityDoesNotHaveDeliveryQueue()
        {
            var testEntity = Substitute.For<IEntity>();
            testEntity.Name.Returns("a");
            testEntity.DeliveryQueue.Returns((DeliveryQueue)null);
            var testEntities = new List<(string Address, IEntity entity)> {
                ("a", testEntity)
            };
            var fakeEntityLookup = Substitute.For<IEntityLookup>();
            fakeEntityLookup.GetEnumerator().Returns(testEntities.GetEnumerator());
            var fakeLoggerProvider = Substitute.For<ILoggerProvider>();
            var fakeHost = Substitute.For<IContainerHost>();

            fakeHost.RegisterManagementProcessors(fakeEntityLookup, fakeLoggerProvider);

            fakeHost.DidNotReceive()
                .RegisterRequestProcessor(
                    Arg.Any<string>(),
                    Arg.Any<ManagementRequestProcessor>()
                );
        }

        [Test]
        public void RegisterManagementProcessors_RegistersManagementProcessor_WhenEntityHasDeliveryQueue()
        {
            var testEntity = Substitute.For<IEntity>();
            testEntity.Name.Returns("zb");
            testEntity.DeliveryQueue.Returns(new DeliveryQueue());
            var testEntities = new List<(string Address, IEntity entity)> {
                ("zb", testEntity)
            };
            var fakeEntityLookup = Substitute.For<IEntityLookup>();
            fakeEntityLookup.GetEnumerator().Returns(testEntities.GetEnumerator());
            var fakeLoggerProvider = Substitute.For<ILoggerProvider>();
            var fakeHost = Substitute.For<IContainerHost>();

            fakeHost.RegisterManagementProcessors(fakeEntityLookup, fakeLoggerProvider);

            fakeHost.ShouldSatisfyAllConditions(
                () => fakeHost
                    .Received(1)
                    .RegisterRequestProcessor("zb/$management", Arg.Any<ManagementRequestProcessor>()),
                () => fakeLoggerProvider
                    .Received(1)
                    .CreateLogger(nameof(ManagementRequestProcessor))
            );
        }

        [Test]
        public void RegisterManagementProcessors_RegistersMultipleManagementProcessor_WhenEntityHasDeliveryQueue()
        {
            var testEntity1 = Substitute.For<IEntity>();
            testEntity1.Name.Returns("zb");
            testEntity1.DeliveryQueue.Returns(new DeliveryQueue());
            var testEntity2 = Substitute.For<IEntity>();
            testEntity2.Name.Returns("ac");
            testEntity2.DeliveryQueue.Returns(new DeliveryQueue());
            var testEntities = new List<(string Address, IEntity entity)> {
                ("zb", testEntity1),
                ("que", Substitute.For<IEntity>()),
                ("zb/Subs/ac", testEntity2)
            };
            var fakeEntityLookup = Substitute.For<IEntityLookup>();
            fakeEntityLookup.GetEnumerator().Returns(testEntities.GetEnumerator());
            var fakeLoggerProvider = Substitute.For<ILoggerProvider>();
            var fakeHost = Substitute.For<IContainerHost>();

            fakeHost.RegisterManagementProcessors(fakeEntityLookup, fakeLoggerProvider);

            fakeHost.ShouldSatisfyAllConditions(
                () => fakeHost
                    .Received(2)
                    .RegisterRequestProcessor(Arg.Any<string>(), Arg.Any<ManagementRequestProcessor>()),
                () => fakeLoggerProvider
                    .Received(2)
                    .CreateLogger(nameof(ManagementRequestProcessor)),
                () => Received.InOrder(() =>
                {
                    fakeHost
                        .Received(1)
                        .RegisterRequestProcessor("zb/$management", Arg.Any<ManagementRequestProcessor>());
                    fakeHost
                        .Received(1)
                        .RegisterRequestProcessor("zb/Subs/ac/$management", Arg.Any<ManagementRequestProcessor>());
                })
            );
        }
    }
}
