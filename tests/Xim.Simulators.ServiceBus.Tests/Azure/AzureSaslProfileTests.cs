using System.Text;
using Amqp;
using Amqp.Sasl;
using Amqp.Types;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Azure
{
    [TestFixture]
    public class AzureSaslProfileTests
    {
        [Test]
        public void Mechanism_ReturnsCorrectMechanism()
        {
            var profile = new AzureSaslProfile();

            profile.Mechanism.ShouldBe((Symbol)"MSSBCBS");
        }

        [Test]
        public void UpgradeTransport_ReturnsTheSameTransportInstance()
        {
            ITransport transport = Substitute.For<ITransport>();
            var profile = new AzureSaslProfileProxy();

            ITransport result = profile.CallUpgradeTransport(transport);

            result.ShouldBeSameAs(transport);
        }

        [Test]
        public void GetStartCommand_ReturnsSaslInitWithCorrectSettings()
        {
            var saslInit = default(SaslInit);
            var profile = new AzureSaslProfileProxy();

            DescribedList command = profile.CallGetStartCommand("hostname1");

            command.ShouldSatisfyAllConditions(
                () => saslInit = command.ShouldBeOfType<SaslInit>(),
                () => saslInit.Mechanism.ShouldBe((Symbol)"MSSBCBS"),
                () => saslInit.InitialResponse.ShouldBe(Encoding.UTF8.GetBytes("MSSBCBS"))
            );
        }

        [Test]
        public void OnCommand_ReturnsSaslOutcomeOk_WhenCommandSaslInit()
        {
            var outcome = default(SaslOutcome);
            var profile = new AzureSaslProfileProxy();

            DescribedList result = profile.CallOnCommand(new SaslInit());

            result.ShouldSatisfyAllConditions(
                () => outcome = result.ShouldBeOfType<SaslOutcome>(),
                () => outcome.Code.ShouldBe(SaslCode.Ok)
            );
        }

        [Test]
        public void OnCommand_ReturnsNull_WhenCommandSaslMechanism()
        {
            var profile = new AzureSaslProfileProxy();

            DescribedList result = profile.CallOnCommand(new SaslMechanisms());

            result.ShouldBeNull();
        }

        [Test]
        public void OnCommand_ReturnsAmqpException_WhenCommandNotSupported()
        {
            var command = new SaslChallenge();
            var profile = new AzureSaslProfileProxy();

            AmqpException exception = Should.Throw<AmqpException>(() => profile.CallOnCommand(command));
            exception.Error.Condition.ShouldBe((Symbol)ErrorCode.NotAllowed);
            exception.Message.ShouldBe(command.ToString());
        }

        private class AzureSaslProfileProxy : AzureSaslProfile
        {
            public ITransport CallUpgradeTransport(ITransport transport)
                => UpgradeTransport(transport);

            public DescribedList CallGetStartCommand(string hostname)
                => GetStartCommand(hostname);

            public DescribedList CallOnCommand(DescribedList command)
                => OnCommand(command);
        }
    }
}
