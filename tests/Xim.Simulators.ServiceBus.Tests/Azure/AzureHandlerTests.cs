using System;
using System.Reflection;
using Amqp;
using Amqp.Handler;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Azure.Tests
{
    [TestFixture]
    public class AzureHandlerTests
    {
        [TestCase(EventId.SendDelivery)]
        public void CanHandle_ReturnsTrue_WhenSupportedEventId(EventId supportedEventId)
        {
            AzureHandler.Instance.CanHandle(supportedEventId).ShouldBeTrue();
        }

        [TestCase(EventId.ReceiveDelivery)]
        [TestCase(EventId.LinkLocalOpen)]
        public void CanHandle_ReturnsFalse_WhenNotSupportedEventId(EventId supportedEventId)
        {
            AzureHandler.Instance.CanHandle(supportedEventId).ShouldBeFalse();
        }

        [Test]
        public void Handle_UpdatesDeliveryTag_WhenEventIdIsSendDeliveryAndContextIsIDelivery()
        {
            var fakeDelivery = Substitute.For<IDelivery>();
            var factory = typeof(Event).GetMethod("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var evt = (Event)factory.Invoke(null, new object[] { EventId.SendDelivery, null, null, null, fakeDelivery });

            AzureHandler.Instance.Handle(evt);

            fakeDelivery.Tag.Length.ShouldBe(Guid.NewGuid().ToByteArray().Length);
        }

        [Test]
        public void Handle_DoesNotUpdateDeliveryTag_WhenEventIdIsNotSendDelivery()
        {
            var fakeDelivery = Substitute.For<IDelivery>();
            fakeDelivery.Tag = null;
            var factory = typeof(Event).GetMethod("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var evt = (Event)factory.Invoke(null, new object[] { EventId.ReceiveDelivery, null, null, null, fakeDelivery });

            AzureHandler.Instance.Handle(evt);

            fakeDelivery.Tag.ShouldBeNull();
        }

        [Test]
        public void Handle_DoesNotThrow_WhenContextIsNotIDelivery()
        {
            var factory = typeof(Event).GetMethod("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var evt = (Event)factory.Invoke(null, new object[] { EventId.SendDelivery, null, null, null, new Message() });

            Should.NotThrow(() => AzureHandler.Instance.Handle(evt));
        }
    }
}
