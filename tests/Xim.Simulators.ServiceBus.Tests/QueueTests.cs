using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class QueueTests
    {
        private static readonly Regex RxValidName = new Regex("^[A-Za-z0-9]$|^[A-Za-z0-9][\\w\\.\\-\\/~]{0,258}[A-Za-z0-9]$", RegexOptions.Compiled);

        [Test]
        public void Constructor_Throws_WhenNameNull()
            => Should.Throw<ArgumentNullException>(() => new Queue(null))
                .ParamName.ShouldBe("name");

        [TestCase("")]
        [TestCase(".abc")]
        [TestCase("abc.")]
        [TestCase("abcabc/")]
        [TestCase("ab^c")]
        [TestCase("a*bc")]
        public void Contructor_Throws_WhenNameInvalid(string invalidName)
        {
            var exception = Should.Throw<ArgumentException>(() => new Queue(invalidName));
            exception.ParamName.ShouldBe("name");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotValid, RxValidName));
        }

        [Test]
        public void Contructor_Throws_WhenNameLongerThan260Characters()
        {
            var invalidName = new string('x', 261);

            var exception = Should.Throw<ArgumentException>(() => new Queue(invalidName));
            exception.ParamName.ShouldBe("name");
            exception.Message.ShouldStartWith(SR.Format(SR.SbEntityNameNotValid, RxValidName));
        }

        [TestCase("a")]
        [TestCase("a0")]
        [TestCase("a-0")]
        [TestCase("a-da_t~a/3.2/av0")]
        public void Contructor_SetsName_WhenNameValid(string name)
        {
            var queue = new Queue(name);

            queue.Name.ShouldBe(name);
        }
    }
}
