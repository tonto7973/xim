using System;
using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Entities;

namespace Xim.Simulators.ServiceBus.Processing.Tests
{
    [TestFixture]
    public class EntityLookupTests
    {
        [Test]
        public void Find_FindsTopicEntityCI()
        {
            var topic = Substitute.For<ITopic, IEntity>();
            topic.Name.Returns("myTopic");
            var topics = new Dictionary<string, ITopic>
            {
                [topic.Name] = topic
            };
            var fakeSimulator = Substitute.For<IServiceBusSimulator>();
            fakeSimulator.Topics.Returns(topics);
            var lookup = new EntityLookup(fakeSimulator);

            lookup.Find("myTopic").ShouldBeSameAs(topic);
            lookup.Find("mytopic").ShouldBeSameAs(topic);
        }

        [Test]
        public void Find_FindsSubscriptionsEntityCI()
        {
            var subscription = Substitute.For<IQueue, IEntity>();
            subscription.Name.Returns("sub");
            var subscriptions = new Dictionary<string, IQueue>
            {
                [subscription.Name] = subscription
            };
            var topic = Substitute.For<ITopic, IEntity>();
            topic.Name.Returns("T");
            topic.Subscriptions.Returns(subscriptions);
            var topics = new Dictionary<string, ITopic>
            {
                [topic.Name] = topic
            };
            var fakeSimulator = Substitute.For<IServiceBusSimulator>();
            fakeSimulator.Topics.Returns(topics);
            var lookup = new EntityLookup(fakeSimulator);

            lookup.Find("T/Subscriptions/sub").ShouldBeSameAs(subscription);
            lookup.Find("t/subscriptions/sub").ShouldBeSameAs(subscription);
        }

        [Test]
        public void Find_FindsQueueEntityCI()
        {
            var queue = Substitute.For<IQueue, IEntity>();
            queue.Name.Returns("myQue");
            var queues = new Dictionary<string, IQueue>
            {
                [queue.Name] = queue
            };
            var fakeSimulator = Substitute.For<IServiceBusSimulator>();
            fakeSimulator.Queues.Returns(queues);
            var lookup = new EntityLookup(fakeSimulator);

            lookup.Find("myQue").ShouldBeSameAs(queue);
            lookup.Find("myque").ShouldBeSameAs(queue);
        }

        [Test]
        public void Find_FindsAllRegisteredEntities()
        {
            var subscription = Substitute.For<IQueue, IEntity>();
            subscription.Name.Returns("bar");
            var subscriptions = new Dictionary<string, IQueue>
            {
                [subscription.Name] = subscription
            };
            var topic = Substitute.For<ITopic, IEntity>();
            topic.Name.Returns("foo");
            topic.Subscriptions.Returns(subscriptions);
            var topics = new Dictionary<string, ITopic>
            {
                [topic.Name] = topic
            };
            var queue = Substitute.For<IQueue, IEntity>();
            queue.Name.Returns("qux");
            var queues = new Dictionary<string, IQueue>
            {
                [queue.Name] = queue
            };
            var fakeSimulator = Substitute.For<IServiceBusSimulator>();
            fakeSimulator.Topics.Returns(topics);
            fakeSimulator.Queues.Returns(queues);
            var lookup = new EntityLookup(fakeSimulator);

            lookup.Find("foo").ShouldBeSameAs(topic);
            lookup.Find("foo/Subscriptions/Bar").ShouldBeSameAs(subscription);
            lookup.Find("qux").ShouldBeSameAs(queue);
            lookup.Find("baz").ShouldBeNull();
        }

        [Test]
        public void GetEnumerator_ReturnsAllEntriesFromTopicAndSubscriptionsAndQueues([Values] bool genericEnumerator)
        {
            var subscription1 = Substitute.For<IQueue, IEntity>();
            subscription1.Name.Returns("sub1");
            var subscription2 = Substitute.For<IQueue, IEntity>();
            subscription2.Name.Returns("sub2");
            var subscriptions = new Dictionary<string, IQueue>
            {
                [subscription1.Name] = subscription1,
                [subscription2.Name] = subscription2
            };
            var topic = Substitute.For<ITopic, IEntity>();
            topic.Name.Returns("topic");
            topic.Subscriptions.Returns(subscriptions);
            var topics = new Dictionary<string, ITopic>
            {
                [topic.Name] = topic
            };
            var queue = Substitute.For<IQueue, IEntity>();
            queue.Name.Returns("que");
            var queues = new Dictionary<string, IQueue>
            {
                [queue.Name] = queue
            };
            var fakeSimulator = Substitute.For<IServiceBusSimulator>();
            fakeSimulator.Topics.Returns(topics);
            fakeSimulator.Queues.Returns(queues);
            var lookup = new EntityLookup(fakeSimulator);
            var data = new Dictionary<string, IEntity>();

            var enumerator = genericEnumerator
                ? lookup.GetEnumerator()
                : ((IEnumerable)lookup).GetEnumerator();
            using (enumerator as IDisposable)
            {
                while (enumerator.MoveNext())
                {
                    var (address, entity) = ((string, IEntity))enumerator.Current;
                    data.Add(address, entity);
                }
            }

            data.ShouldSatisfyAllConditions(
                () => data.Count.ShouldBe(4),
                () => data["que"].ShouldBeSameAs(queue),
                () => data["topic"].ShouldBeSameAs(topic),
                () => data["topic/Subscriptions/sub1"].ShouldBeSameAs(subscription1),
                () => data["topic/Subscriptions/sub2"].ShouldBeSameAs(subscription2)
            );
        }
    }
}
