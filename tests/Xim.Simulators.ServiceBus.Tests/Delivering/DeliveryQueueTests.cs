using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Listener;
using Amqp.Types;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Delivering.Tests
{
    [TestFixture]
    public class DeliveryQueueTests
    {
        [Test]
        public void Enqueue_AddsDeliveryToQueue()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            var delivery = new Delivery(new Message());
            var queue = new DeliveryQueue();

            queue.Enqueue(delivery);

            queue.Dequeue(cts.Token).ShouldBeSameAs(delivery.Message);
        }

        [Test]
        public void Enqueue_SetsMessageSequence()
        {
            var message = new Message();
            var queue = new DeliveryQueue();

            queue.Enqueue(new Delivery(message));

            message.MessageAnnotations[(Symbol)"x-opt-sequence-number"].ShouldBe(1L);
        }

        [Test]
        public void Enqueue_SetsMessageSequenceInOrder()
        {
            var message1 = new Message();
            var message2 = new Message();
            var queue = new DeliveryQueue();

            queue.Enqueue(new Delivery(message1));
            queue.Enqueue(new Delivery(message2));

            message1.MessageAnnotations[(Symbol)"x-opt-sequence-number"].ShouldBe(1L);
            message2.MessageAnnotations[(Symbol)"x-opt-sequence-number"].ShouldBe(2L);
        }

        [Test]
        public async Task Enqueue_SetsUniqueMessageForSimultaniousDeliveries()
        {
            var queue = new DeliveryQueue();
            var messages = Enumerable.Range(1, 16).Select(_ => new Message()).ToArray();
            var enqueueTasks = messages
                .Select(message => Task.Run(() => queue.Enqueue(new Delivery(message))));

            await Task.WhenAll(enqueueTasks);

            var sequenceIds = messages
                .Select(message => (long)message.MessageAnnotations[(Symbol)"x-opt-sequence-number"])
                .ToArray();
            sequenceIds.ShouldBeUnique();
        }

        [Test]
        public void Dequeue_ThrowsWhenNoDeliveryInQueueWithinTimeout()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
            var queue = new DeliveryQueue();

            Action action = () => queue.Dequeue(cts.Token);

            action.ShouldThrow<OperationCanceledException>();
        }

        [Test]
        public void Process_DoesNotProcessDeliveryIfItDoesntExist()
        {
            var message = new Message();
            message.InitializePrivateProperty("Delivery");
            var link = Construct.Uninitialized<ListenerLink>();
            var messageContext = Construct.ForPrivate<MessageContext>(link, message);
            var queue = new DeliveryQueue();

            Should.NotThrow(() => queue.Process(messageContext));
        }

        [Test]
        public void Process_ProcessesDelivery()
        {
            var message = new Message();
            message.InitializePrivateProperty("Delivery");
            var link = Construct.Uninitialized<ListenerLink>();
            var messageContext = Construct.ForPrivate<MessageContext>(link, message);
            var delivery = new Delivery(message);
            var queue = new DeliveryQueue();
            queue.Enqueue(delivery);
            queue.Dequeue(CancellationToken.None);

            queue.Process(messageContext);

            delivery.Processed.ShouldNotBeNull();
        }

        [Test]
        public void Enqueue_GetsFirstInsertedItem()
        {
            var delivery1 = new Delivery(new Message());
            var delivery2 = new Delivery(new Message());
            var queue = new DeliveryQueue();

            queue.Enqueue(delivery1);
            queue.Enqueue(delivery2);

            queue.Dequeue(CancellationToken.None).ShouldBeSameAs(delivery1.Message);
        }

        [Test]
        public void Dispose_DisposesQueue()
        {
            var delivery = new Delivery(new Message());
            var queue = new DeliveryQueue();

            queue.Dispose();

            Should.Throw<ObjectDisposedException>(() => queue.Enqueue(delivery));
            Should.Throw<ObjectDisposedException>(() => queue.Dequeue(CancellationToken.None));
            Should.Throw<ObjectDisposedException>(() => queue.Process(null));
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var queue = new DeliveryQueue();

            Should.NotThrow(() =>
            {
                queue.Dispose();
                queue.Dispose();
                queue.Dispose();
            });
        }
    }
}
