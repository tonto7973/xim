using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Entities.Tests
{
    [TestFixture]
    public class TopicEntityTests
    {
        [TestCase("abc", "sub1")]
        [TestCase("def", "2sub")]
        public void Constructor_InitializesInstance(string name, string sub)
        {
            var entity = new TopicEntity(name, new[] { sub });

            entity.ShouldSatisfyAllConditions(
                () => entity.Name.ShouldBe(name),
                () => entity.Subscriptions.ShouldHaveSingleItem(),
                () => entity.Subscriptions.TryGetValue(sub, out _).ShouldBeTrue()
            );
        }

        [Test]
        public void Post_Throws_WhenMessageNull()
        {
            var entity = new TopicEntity("", new string[0]);

            Should.Throw<ArgumentNullException>(() => entity.Post(null))
                .ParamName.ShouldBe("message");
        }

        [Test]
        public void Post_PostsClonesToSubscriptions()
        {
            var message = new Amqp.Message();
            var entity = new TopicEntity("nm", new[] { "xyz", "007" });

            entity.Post(message);

            entity.ShouldSatisfyAllConditions(
                () => ((IEntity)entity.Subscriptions["xYz"])
                    .DeliveryQueue
                    .Dequeue(CancellationToken.None)
                    .ShouldNotBeSameAs(message),
                () => ((IEntity)entity.Subscriptions["007"])
                    .DeliveryQueue
                    .Dequeue(CancellationToken.None)
                    .ShouldNotBeSameAs(message)
            );
        }

        [Test]
        public void Deliveries_ReturnsAllPostedMessages()
        {
            var message1 = new Amqp.Message();
            var message2 = new Amqp.Message();
            var entity = new TopicEntity("a", new[] { "b" });

            entity.Post(message1);
            entity.Post(message2);

            entity.Deliveries.ShouldSatisfyAllConditions(
                () => entity.Deliveries.Count.ShouldBe(2),
                () => entity.Deliveries.ShouldContain(a => a.Message == message1),
                () => entity.Deliveries.ShouldContain(a => a.Message == message2)
            );
        }

        [Test]
        public void Post_Throws_WhenObjectDisposed()
        {
            var entity = new TopicEntity("a", new string[0]);

            entity.Dispose();

            Should.Throw<ObjectDisposedException>(() => entity.Post(new Amqp.Message()));
        }

        [Test]
        public void IEntityPost_PostsClonesToSubscriptions()
        {
            var message = new Amqp.Message();
            var entity = new TopicEntity("a", new[] { "fE" });

            ((IEntity)entity).Post(message);

            ((IEntity)entity.Subscriptions["Fe"])
                    .DeliveryQueue
                    .Dequeue(CancellationToken.None)
                    .ShouldNotBeSameAs(message);
        }

        [Test]
        public void IEntityPost_Throws_WhenMessageNull()
        {
            var entity = (IEntity)new TopicEntity("a", new string[0]);

            Should.Throw<ArgumentNullException>(() => entity.Post(null))
                .ParamName.ShouldBe("message");
        }

        [Test]
        public void IEntityPost_Throws_WhenObjectDisposed()
        {
            var entity = new TopicEntity("a", new string[0]);

            entity.Dispose();

            Should.Throw<ObjectDisposedException>(() => ((IEntity)entity).Post(new Amqp.Message()));
        }

        [TestCase("queue-online-21234")]
        [TestCase("abc")]
        public void ToString_ReturnsEntityName(string name)
        {
            var entity = new TopicEntity(name, new string[0]);

            entity.ToString().ShouldBe(name);
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var entity = new TopicEntity("f", new[] { "g" });

            entity.Post(new Amqp.Message());

            Should.NotThrow(() =>
            {
                entity.Dispose();
                entity.Dispose();
            });
        }

        [Test]
        public void IEntityDeliveryQueue_ReturnsNull()
        {
            var entity = (IEntity)new TopicEntity("z", new string[0]);

            entity.DeliveryQueue.ShouldBeNull();
        }
    }
}
