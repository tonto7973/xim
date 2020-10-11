using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class TopicTests
    {
        private static readonly Regex RxValidName = new Regex("^[A-Za-z0-9]$|^[A-Za-z0-9][\\w\\.\\-\\/~]{0,258}[A-Za-z0-9]$", RegexOptions.Compiled);

        [Test]
        public void Contructor_Throws_WhenNameNull()
            => Should.Throw<ArgumentNullException>(() => new Topic(null))
                .ParamName.ShouldBe("name");

        [TestCase("")]
        [TestCase(".abc")]
        [TestCase("abc.")]
        [TestCase("abcabc/")]
        [TestCase("ab^c")]
        [TestCase("a*bc")]
        public void Contructor_Throws_WhenNameInvalid(string invalidName)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => new Topic(invalidName));

            exception.ParamName.ShouldBe("name");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotValid, RxValidName));
        }

        [Test]
        public void Contructor_Throws_WhenNameLongerThan260Characters()
        {
            var invalidName = new string('x', 261);

            ArgumentException exception = Should.Throw<ArgumentException>(() => new Topic(invalidName));

            exception.ParamName.ShouldBe("name");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotValid, RxValidName));
        }

        [TestCase("a")]
        [TestCase("a0")]
        [TestCase("a-0")]
        [TestCase("a-da_t~a/3.2/av0")]
        public void Contructor_SetsName_WhenNameValid(string name)
        {
            var topic = new Topic(name);

            topic.Name.ShouldBe(name);
        }

        [TestCase(null)]
        [TestCase(0)]
        public void Constructor_SetsSubscriptionsInstance_WhenNoSubscriptions(int? set)
        {
            Subscription[] subs = set.HasValue ? new Subscription[set.Value] : null;
            var topic = new Topic("a", subs);

            topic.Subscriptions.ShouldNotBeNull();
        }

        [Test]
        public void Constructor_SetsSubscriptions()
        {
            var sub1 = new Subscription("b");
            var sub2 = new Subscription("x");
            var topic = new Topic("a", sub1, sub2);

            topic.ShouldSatisfyAllConditions(
                () => topic.Subscriptions.Count.ShouldBe(2),
                () => topic.Subscriptions[0].ShouldBeSameAs(sub1),
                () => topic.Subscriptions[1].ShouldBeSameAs(sub2)
            );
        }

        [Test]
        public void Constructor_Throws_WhenAnySubscriptionNull()
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => new Topic("a", new Subscription("x"), null));

            exception.ParamName.ShouldBe("subscriptions");
            exception.Message.ShouldStartWith(SR.Format(SR.SbTopicSubscriptionNull));
        }

        [Test]
        public void Constructor_Throws_WhenSubscriptionsNotDistinctCaseInsensitive()
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => new Topic("a", new Subscription("x"), new Subscription("X")));

            exception.ParamName.ShouldBe("subscriptions");
            exception.Message.ShouldStartWith(SR.Format(SR.SbTopicSubscriptionNotUnique, "X"));
        }

        [Test]
        public void Subscriptions_CannotBeModified()
        {
            var topic = new Topic("a", new Subscription("ok"));

            Should.Throw<NotSupportedException>(() => ((IList<Subscription>)topic.Subscriptions)[0] = null);
        }
    }
}
