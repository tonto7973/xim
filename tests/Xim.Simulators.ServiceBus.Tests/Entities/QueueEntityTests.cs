using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Entities.Tests
{
    [TestFixture]
    public class QueueEntityTests
    {
        [TestCase("a")]
        [TestCase("Sub")]
        public void Constructor_InitializesInstance(string name)
        {
            var entity = new QueueEntity(name);

            entity.Name.ShouldBe(name);
        }

        [Test]
        public void Post_Throws_WhenMessageNull()
        {
            var entity = new QueueEntity("s");

            Should.Throw<ArgumentNullException>(() => entity.Post(null))
                .ParamName.ShouldBe("message");
        }

        [Test]
        public void IEntityPost_Throws_WhenMessageNull()
        {
            var entity = (IEntity)new QueueEntity("a");

            Should.Throw<ArgumentNullException>(() => entity.Post(null))
                .ParamName.ShouldBe("message");
        }

        [Test]
        public void Post_CreatesEndEnqueuesDelivery()
        {
            var message = new Amqp.Message();
            var entity = new QueueEntity("s");

            var delivery = entity.Post(message);

            entity.ShouldSatisfyAllConditions(
                () => delivery.Message.ShouldBeSameAs(message),
                () => ((IEntity)entity).DeliveryQueue.Dequeue(CancellationToken.None).ShouldBeSameAs(message)
            );
        }

        [Test]
        public void IEntityPost_CreatesEndEnqueuesDelivery()
        {
            var message = new Amqp.Message();
            var entity = (IEntity)new QueueEntity("s");

            entity.Post(message);

            entity.DeliveryQueue.Dequeue(CancellationToken.None).ShouldBeSameAs(message);
        }

        [TestCase("queue-online-21234")]
        [TestCase("abc")]
        public void ToString_ReturnsEntityName(string name)
        {
            var entity = new QueueEntity(name);

            entity.ToString().ShouldBe(name);
        }

        [Test]
        public void Post_Throws_WhenObjectDisposed()
        {
            var entity = new QueueEntity("s");

            entity.Dispose();

            Should.Throw<ObjectDisposedException>(() => entity.Post(new Amqp.Message()));
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var entity = new QueueEntity("s");
            entity.Post(new Amqp.Message());

            Should.NotThrow(() =>
            {
                entity.Dispose();
                entity.Dispose();
                entity.Dispose();
            });
        }
    }
}
