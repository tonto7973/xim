using System;
using System.Threading.Tasks;
using Amqp;
using Amqp.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Processing.Tests
{
    [TestFixture]
    public class ManagementRequestProcessorTests
    {
        [Test]
        public void Constructor_CreatesValidLogger()
        {
            ILoggerProvider fakeLoggerProvider = Substitute.For<ILoggerProvider>();

            new ManagementRequestProcessor(fakeLoggerProvider);

            fakeLoggerProvider.Received(1).CreateLogger("ManagementRequestProcessor");
        }

        [Test]
        public void Credit_Returns100()
        {
            var processor = new ManagementRequestProcessor(Substitute.For<ILoggerProvider>());

            processor.Credit.ShouldBe(100);
        }

        [Test]
        public async Task Process_ReturnsSuccessStatusCodeInResponse()
        {
            var processor = new ManagementRequestProcessor(Substitute.For<ILoggerProvider>());
            var request = new Message();

            Message response = await TestAmqpHost.ProcessManagementRequestAsync(request, processor);

            response.ApplicationProperties["statusCode"].ShouldBe(200);
        }

        [TestCase("foobar")]
        [TestCase("com.microsoft:blah")]
        public async Task Process_LogsUnsupportedOperation(string operation)
        {
            ILogger fakeLogger = Substitute.For<ILogger>();
            ILoggerProvider fakeLoggerProvider = Substitute.For<ILoggerProvider>();
            fakeLoggerProvider.CreateLogger(Arg.Any<string>()).Returns(fakeLogger);
            var processor = new ManagementRequestProcessor(fakeLoggerProvider);
            var request = new Message
            {
                ApplicationProperties = new Amqp.Framing.ApplicationProperties
                {
                    ["operation"] = operation
                }
            };

            await TestAmqpHost.ProcessManagementRequestAsync(request, processor);

            fakeLogger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<FormattedLogValues>(a => a.ToString() == $"Unsupported operation {operation}."),
                Arg.Any<Exception>(),
                Arg.Any<Func<FormattedLogValues, Exception, string>>());
        }

        [Test]
        public async Task Process_ProcessesRenewLock()
        {
            ILogger fakeLogger = Substitute.For<ILogger>();
            ILoggerProvider fakeLoggerProvider = Substitute.For<ILoggerProvider>();
            fakeLoggerProvider.CreateLogger(Arg.Any<string>()).Returns(fakeLogger);
            var processor = new ManagementRequestProcessor(fakeLoggerProvider);
            var lockTokens = new Guid[] { Guid.NewGuid(), Guid.NewGuid() };
            var request = new Message(new Map { ["lock-tokens"] = lockTokens })
            {
                ApplicationProperties = new Amqp.Framing.ApplicationProperties
                {
                    ["operation"] = "com.microsoft:renew-lock"
                }
            };

            Message response = await TestAmqpHost.ProcessManagementRequestAsync(request, processor);
            var expirations = (response.Body as Map)?["expirations"] as DateTime[];

            response.ApplicationProperties["statusCode"].ShouldBe(200);
            expirations.Length.ShouldBe(2);
            fakeLogger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<FormattedLogValues>(a => a.ToString() == "com.microsoft:renew-lock applied to 2 lock token(s)."),
                Arg.Any<Exception>(),
                Arg.Any<Func<FormattedLogValues, Exception, string>>());
        }
    }
}
