using System;
using Amqp;
using Amqp.Listener;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Entities;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Processing.Endpoints.Tests
{
    [TestFixture]
    public class IncomingLinkEndpointTests
    {
        [Test]
        public void OnMessage_PostsMessageToEntityAndCompletesMessageContext()
        {
            var message = new Message();
            message.InitializePrivateProperty("Delivery");
            ListenerLink link = Construct.Uninitialized<ListenerLink>();
            MessageContext messageContext = Construct.ForPrivate<MessageContext>(link, message);
            IEntity entity = Substitute.For<IEntity>();
            var endpoint = new IncomingLinkEndpoint(entity);

            endpoint.OnMessage(messageContext);

            endpoint.ShouldSatisfyAllConditions(
                () => entity.Received(1).Post(Arg.Is<Message>(m => !ReferenceEquals(m, message))),
                () => messageContext.State.ShouldBe(ContextState.Completed)
            );
        }

        [Test]
        public void OnMessage_DoesNotCompleteMessageContext_WhenPostingFails()
        {
            var message = new Message();
            message.InitializePrivateProperty("Delivery");
            ListenerLink link = Construct.Uninitialized<ListenerLink>();
            MessageContext messageContext = Construct.ForPrivate<MessageContext>(link, message);
            IEntity entity = Substitute.For<IEntity>();
            entity.When(instance => instance.Post(Arg.Any<Message>()))
                  .Do(_ => throw new NotSupportedException());
            var endpoint = new IncomingLinkEndpoint(entity);

            Action action = () => endpoint.OnMessage(messageContext);

            endpoint.ShouldSatisfyAllConditions(
                () => action.ShouldThrow<NotSupportedException>(),
                () => messageContext.State.ShouldBe(ContextState.Active)
            );
        }

        [Test]
        public void OnFlow_Throws()
        {
            var endpoint = new IncomingLinkEndpoint(Substitute.For<IEntity>());

            Should.Throw<NotSupportedException>(() => endpoint.OnFlow(null));
        }

        [Test]
        public void OnDisposition_Throws()
        {
            var endpoint = new IncomingLinkEndpoint(Substitute.For<IEntity>());

            Should.Throw<NotSupportedException>(() => endpoint.OnDisposition(null));
        }
    }
}
