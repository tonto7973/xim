using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amqp.Framing;
using Amqp.Transactions;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Delivering.Tests
{
    [TestFixture]
    public class DeliveryTests
    {
        [Test]
        public void ImplementsIDelivery()
            => typeof(IDelivery)
                .IsAssignableFrom(typeof(Delivery))
                .ShouldBeTrue();

        [Test]
        public void Constructor_InitializesInstance()
        {
            var message = new Amqp.Message();
            var delivery = new Delivery(message);

            delivery.ShouldSatisfyAllConditions(
                () => delivery.Processed.ShouldBeNull(),
                () => delivery.State.ShouldBeNull(),
                () => delivery.Result.ShouldBeNull(),
                () => delivery.Posted.ShouldBeLessThanOrEqualTo(DateTime.UtcNow)
            );
        }

        [Test]
        public void Process_SetsProcessedAndStateProperties()
        {
            var state = new Rejected();
            var delivery = new Delivery(new Amqp.Message());

            delivery.Process(state);

            delivery.ShouldSatisfyAllConditions(
                () => delivery.Processed.ShouldNotBeNull(),
                () => delivery.State.ShouldBeSameAs(state)
            );
        }

        [TestCase(typeof(Rejected), DeliveryResult.DeadLettered)]
        [TestCase(typeof(Modified), DeliveryResult.Abandoned)]
        [TestCase(typeof(Accepted), DeliveryResult.Completed)]
        [TestCase(typeof(Released), DeliveryResult.Lost)]
        [TestCase(typeof(Declared), DeliveryResult.Unknown)]
        public void Process_SetsCorrectResult_WhenDeliveryStateSet(Type statusType, DeliveryResult expectedResult)
        {
            var state = (DeliveryState)Activator.CreateInstance(statusType);
            var delivery = new Delivery(new Amqp.Message());

            delivery.Process(state);

            delivery.Result.ShouldBe(expectedResult);
        }

        [Test]
        public async Task Wait_ReturnsFalse_WhenProcessNotCompletedWithinTimeout()
        {
            var delivery = new Delivery(new Amqp.Message());

            var result = await delivery.WaitAsync(TimeSpan.FromMilliseconds(1));

            result.ShouldBeFalse();
        }

        [Test]
        public async Task Wait_ReturnsTrue_WhenProcessCompletedBeforeTimeout()
        {
            var delivery = new Delivery(new Amqp.Message());
            var timeout = TimeSpan.FromSeconds(15);
            var sw = new Stopwatch();

            sw.Start();
            Task<bool> task = delivery.WaitAsync(timeout);
            delivery.Process(new Rejected());
            var result = await task;
            sw.Stop();

            delivery.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => sw.Elapsed.ShouldBeLessThan(timeout)
            );
        }

        [Test]
        public void Wait_CanCallMultipleTimes_WhenProcessCompleted()
        {
            var delivery = new Delivery(new Amqp.Message());

            Task<bool> task = delivery.WaitAsync(TimeSpan.FromSeconds(15));
            delivery.Process(new Rejected());

            Task.WhenAll(task, delivery.WaitAsync(TimeSpan.FromSeconds(15)))
                .ShouldNotThrow();
        }

        [Test]
        public async Task Wait_Cancels_WhenOperationCancelledBeforeTimeout()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
            var delivery = new Delivery(new Amqp.Message());

            await delivery
                .WaitAsync(TimeSpan.FromSeconds(15), cts.Token)
                .ShouldCancelOperationAsync();
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var delivery = new Delivery(new Amqp.Message());

            Action action = () =>
            {
                delivery.Dispose();
                delivery.Dispose();
                delivery.Dispose();
            };

            action.ShouldNotThrow();
        }

        [Test]
        public async Task Dispose_DisposesWaitHandleAndCancelsSubsequentWaitOperation()
        {
            var delivery = new Delivery(new Amqp.Message());

            delivery.Dispose();

            await delivery
                .WaitAsync(TimeSpan.FromMilliseconds(1))
                .ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public async Task Dispose_DisposesWaitHandleAndCancelsCurrentWaitOperation()
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var delivery = new Delivery(new Amqp.Message());
            Task task1 = Task
                .Delay(delay)
                .ContinueWith(_ => delivery.Dispose());
            Task<bool> task2 = delivery.WaitAsync(TimeSpan.FromSeconds(2));

            await Task
                .WhenAll(task1, task2)
                .ShouldThrowAsync<ObjectDisposedException>();
        }
    }
}
