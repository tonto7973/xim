using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Delivering;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Processing.Endpoints.Tests
{
    [TestFixture]
    public class OutgoingLinkEndpointTests
    {
        [Test]
        public async Task OnFlow_StartsSendingMessagesAsynchronously()
        {
            var backingQueue = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            ListenerLink link = Construct.Uninitialized<ListenerLink>();
            FlowContext flowContext = Construct.ForPrivate<FlowContext>(link, 1, new Fields());
            IDeliveryQueue queue = Substitute.For<IDeliveryQueue>();
            queue.Dequeue(Arg.Any<CancellationToken>())
                 .Returns(ci => backingQueue.Take(ci.Arg<CancellationToken>()));
            var endpoint = new OutgoingLinkEndpoint(queue);

            endpoint.OnFlow(flowContext);
            backingQueue.Add(new Message());

            await Task.Delay(500);

            queue.Received(1).Dequeue(Arg.Any<CancellationToken>());
            backingQueue.ShouldBeEmpty();
        }

        [Test]
        public void OnFlow_CancelsPreviousTask_WhenCalledMultipleTimes()
        {
            ListenerLink link = Construct.Uninitialized<ListenerLink>();
            FlowContext flowContext = Construct.ForPrivate<FlowContext>(link, 1, new Fields());
            IDeliveryQueue queue = Substitute.For<IDeliveryQueue>();
            var endpoint = new OutgoingLinkEndpoint(queue);

            endpoint.OnFlow(flowContext);

            Should.NotThrow(() => endpoint.OnFlow(flowContext));
        }

        [Test]
        public void OnMessage_NotSupported()
        {
            IDeliveryQueue queue = Substitute.For<IDeliveryQueue>();
            var endpoint = new OutgoingLinkEndpoint(queue);

            Should.Throw<NotSupportedException>(() => endpoint.OnMessage(null));
        }

        [Test]
        public void OnLinkClosed_CancelsSendingMessages()
        {
            var backingQueue = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            ListenerLink link = Construct.Uninitialized<ListenerLink>();
            FlowContext flowContext = Construct.ForPrivate<FlowContext>(link, 1, new Fields());
            IDeliveryQueue queue = Substitute.For<IDeliveryQueue>();
            queue.Dequeue(Arg.Any<CancellationToken>())
                 .Returns(ci => backingQueue.Take(ci.Arg<CancellationToken>()));
            var endpoint = new OutgoingLinkEndpoint(queue);

            endpoint.OnFlow(flowContext);
            endpoint.OnLinkClosed(null, null);
            backingQueue.Add(new Message());
            Thread.Sleep(10);

            backingQueue.Count.ShouldBe(1);
        }

        [Test]
        public void OnDisposition_CallsIDeliveryQueueProcess()
        {
            var message = new Message();
            message.InitializePrivateProperty("Delivery");
            IDeliveryQueue queue = Substitute.For<IDeliveryQueue>();
            DispositionContext dispositionContext = Construct.ForPrivate<DispositionContext>(
                Construct.Uninitialized<ListenerLink>(),
                message,
                new Accepted(),
                true
                );
            var endpoint = new OutgoingLinkEndpoint(queue);

            endpoint.OnDisposition(dispositionContext);

            queue.Received(1).Process(dispositionContext);
            dispositionContext.State.ShouldBe(ContextState.Completed);
        }

        [Test]
        public async Task OnFlow_SendsMessageToReceiverLink()
        {
            var message = new Message("x")
            {
                Properties = new Properties
                {
                    CorrelationId = "abc123"
                }
            };
            IDeliveryQueue fakeDeliveryQueue = Substitute.For<IDeliveryQueue>();
            fakeDeliveryQueue
                .Dequeue(Arg.Any<CancellationToken>())
                .Returns(message);
            var endpoint = new OutgoingLinkEndpoint(fakeDeliveryQueue);
            ReceiverLink receiver = await TestAmqpHost.OpenAndLinkReceiverAsync(endpoint);
            try
            {
                receiver.SetCredit(1, CreditMode.Manual);

                Message receivedMessage = await receiver.ReceiveAsync();

                receivedMessage.Properties.CorrelationId
                    .ShouldBe(message.Properties.CorrelationId);
            }
            finally
            {
                await receiver.Session.Connection.CloseAsync();
            }
        }

        [Test]
        public async Task OnFlow_SendsMessageWhenFlowSendTwice()
        {
            var message = new Message("x")
            {
                Properties = new Properties
                {
                    CorrelationId = "abc123"
                }
            };
            IDeliveryQueue fakeDeliveryQueue = Substitute.For<IDeliveryQueue>();
            fakeDeliveryQueue
                .Dequeue(Arg.Any<CancellationToken>())
                .Returns(message);
            var endpoint = new OutgoingLinkEndpoint(fakeDeliveryQueue);
            ReceiverLink receiver = await TestAmqpHost.OpenAndLinkReceiverAsync(endpoint);
            try
            {
                receiver.SetCredit(1, CreditMode.Manual);

                Message receivedMessage = await receiver.ReceiveAsync();
                receiver.Accept(receivedMessage);

                receiver.SetCredit(1, CreditMode.Manual);
                receivedMessage = await receiver.ReceiveAsync();

                receivedMessage.Properties.CorrelationId
                    .ShouldBe(message.Properties.CorrelationId);
            }
            finally
            {
                await receiver.Session.Connection.CloseAsync();
            }
        }
    }
}
