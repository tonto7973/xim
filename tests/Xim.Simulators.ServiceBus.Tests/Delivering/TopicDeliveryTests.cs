using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Delivering.Tests
{
    [TestFixture]
    public class TopicDeliveryTests
    {
        [Test]
        public void ImplementsITopicDelivery()
            => typeof(ITopicDelivery)
                .IsAssignableFrom(typeof(TopicDelivery))
                .ShouldBeTrue();

        [Test]
        public void Constructor_InitializesInstance()
        {
            var message = new Amqp.Message();
            IDelivery[] subscriptions = Enumerable.Empty<IDelivery>().ToArray();
            var delivery = new TopicDelivery(message, subscriptions);

            delivery.ShouldSatisfyAllConditions(
                () => delivery.Posted.Kind.ShouldBe(DateTimeKind.Utc),
                () => delivery.Posted.ShouldBeLessThanOrEqualTo(DateTime.UtcNow),
                () => delivery.Message.ShouldBeSameAs(message),
                () => delivery.Subscriptions.ShouldBeSameAs(subscriptions)
            );
        }

        [Test]
        public async Task Wait_ReturnsTrue_WhenSubscriptionsEmpty()
        {
            var message = new Amqp.Message();
            var subscriptions = new IDelivery[0];
            var delivery = new TopicDelivery(message, subscriptions);

            var result = await delivery.WaitAsync(TimeSpan.FromMilliseconds(1));

            result.ShouldBeTrue();
        }

        [Test]
        public async Task Wait_ReturnsTrue_WhenAllSubscriptionWaitsReturnTrueWithinTimeout()
        {
            var message = new Amqp.Message();
            IDelivery[] subscriptions = Enumerable
                .Range(0, 3)
                .Select(_ =>
                {
                    IDelivery subscription = Substitute.For<IDelivery>();
                    subscription.WaitAsync(Arg.Any<TimeSpan>()).Returns(true);
                    return subscription;
                })
                .ToArray();
            var delivery = new TopicDelivery(message, subscriptions);

            var result = await delivery.WaitAsync(TimeSpan.FromMilliseconds(1));

            result.ShouldBeTrue();
        }

        [Test]
        public async Task Wait_ReturnsFalse_WhenAnySubscriptionWaitsReturnFalseWithinTimeout()
        {
            var message = new Amqp.Message();
            IDelivery[] subscriptions = Enumerable
                .Range(0, 3)
                .Select((_, index) =>
                {
                    IDelivery subscription = Substitute.For<IDelivery>();
                    subscription.WaitAsync(Arg.Any<TimeSpan>()).Returns(index != 1);
                    return subscription;
                })
                .ToArray();
            var delivery = new TopicDelivery(message, subscriptions);

            var result = await delivery.WaitAsync(TimeSpan.FromMilliseconds(1));

            result.ShouldBeFalse();
        }

        [Test]
        public async Task Wait_Cancels_WhenAnySubscriptionWaitsCancels()
        {
            var message = new Amqp.Message();
            IDelivery[] subscriptions = Enumerable
                .Range(0, 3)
                .Select((_, index) =>
                {
                    IDelivery subscription = Substitute.For<IDelivery>();
                    subscription
                        .WaitAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                        .Returns(async call =>
                        {
                            TimeSpan delay = index != 1 ? call.ArgAt<TimeSpan>(0) : TimeSpan.FromSeconds(15);
                            CancellationToken cancellationToken = call.ArgAt<CancellationToken>(1);
                            await Task.Delay(delay.Add(TimeSpan.FromMilliseconds(1)), cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();
                            return true;
                        });
                    return subscription;
                })
                .ToArray();
            var delivery = new TopicDelivery(message, subscriptions);
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
            Task<bool> task = delivery.WaitAsync(TimeSpan.FromMilliseconds(1), cts.Token);

            await task.ShouldCancelOperationAsync();
        }

        [Test]
        public void Wait_Throws_WhenObjectDisposed()
        {
            var delivery = new TopicDelivery(new Amqp.Message(), new List<IDelivery>());

            delivery.Dispose();

            Should.ThrowAsync<ObjectDisposedException>(delivery.WaitAsync(TimeSpan.Zero));
        }

        [Test]
        public void Dispose_DisposesSubscriptions()
        {
            IDisposable subscription = Substitute.For<IDisposable, IDelivery>();
            var delivery = new TopicDelivery(new Amqp.Message(), new List<IDelivery> { (IDelivery)subscription });

            delivery.Dispose();

            subscription.Received(1).Dispose();
        }

        [Test]
        public void Dispose_DoesNotThrow_WhenSubscriptionNotDisposable()
        {
            IDelivery subscription = Substitute.For<IDelivery>();
            var delivery = new TopicDelivery(new Amqp.Message(), new List<IDelivery> { subscription });

            Should.NotThrow(() => delivery.Dispose());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var delivery = new TopicDelivery(new Amqp.Message(), new List<IDelivery>());

            Should.NotThrow(() =>
            {
                delivery.Dispose();
                delivery.Dispose();
            });
        }
    }
}
