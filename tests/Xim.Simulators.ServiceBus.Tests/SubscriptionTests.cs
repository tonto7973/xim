using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class SubscriptionTests
    {
        private static readonly Regex RxValidName = new Regex("^[A-Za-z0-9]$|^[A-Za-z0-9][\\w\\.\\-]{0,48}[A-Za-z0-9]$", RegexOptions.Compiled);

        [Test]
        public void Constructor_Throws_WhenNameNull()
            => Should.Throw<ArgumentNullException>(() => new Subscription(null))
                .ParamName.ShouldBe("name");

        [TestCase("")]
        [TestCase(".abc")]
        [TestCase("abc.")]
        [TestCase("a~bc")]
        [TestCase("ab/c")]
        [TestCase("ab\\c")]
        [TestCase("01234657890123465789012346578901234657890123465789a")]
        public void Constructor_Throws_WhenNameInvalid(string invalidName)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => new Subscription(invalidName));
            exception.ParamName.ShouldBe("name");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotValid, RxValidName));
        }

        [TestCase("a")]
        [TestCase("0")]
        [TestCase("a0")]
        [TestCase("a-._0")]
        [TestCase("01234657890123465789012346578901234657890123465789")]
        public void Constructor_SetsName_WhenNameValid(string name)
        {
            var subscription = new Subscription(name);

            subscription.Name.ShouldBe(name);
        }
    }
}
