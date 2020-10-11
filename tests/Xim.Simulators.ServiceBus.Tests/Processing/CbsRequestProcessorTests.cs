using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Security;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Processing.Tests
{
    [TestFixture]
    public class CbsRequestProcessorTests
    {
        [Test]
        public void Constructor_CreatesValidLogger()
        {
            ILoggerProvider fakeLoggerProvider = Substitute.For<ILoggerProvider>();

            new CbsRequestProcessor(Substitute.For<ISecurityContext>(), fakeLoggerProvider, Substitute.For<ITokenValidator>());

            fakeLoggerProvider.Received(1).CreateLogger("CbsRequestProcessor");
        }

        [Test]
        public void Credit_ReturnsOneHundred_WhenMultipleCalls()
        {
            var processor = new CbsRequestProcessor(Substitute.For<ISecurityContext>(), Substitute.For<ILoggerProvider>(), CbsTokenValidator.Default);

            processor.Credit.ShouldBe(100);
            processor.Credit.ShouldBe(100);
        }

        [Test]
        public async Task Process_CompleteWith200Ok_WhenTokenValid()
        {
            const string testMessageId = "rekwest1025847";
            var processor = new CbsRequestProcessor(Substitute.For<ISecurityContext>(), Substitute.For<ILoggerProvider>(), Substitute.For<ITokenValidator>());

            IDictionary<string, object> responseProperties = await TestAmqpHost.ProcessCbsRequestAsync(testMessageId, processor);

            responseProperties.ShouldBe(new Dictionary<string, object>
            {
                ["CorrelationId"] = testMessageId,
                ["status-code"] = 200
            });
        }

        [Test]
        public async Task Process_CompleteWith401Unathorized_WhenTokenValid()
        {
            const string testMessageId = "someErrorMessageId";
            ITokenValidator fakeTokenValidator = Substitute.For<ITokenValidator>();
            fakeTokenValidator
                .When(instance => instance.Validate(Arg.Any<string>()))
                .Do(_ => throw new ArgumentException("Test"));
            var processor = new CbsRequestProcessor(Substitute.For<ISecurityContext>(), Substitute.For<ILoggerProvider>(), fakeTokenValidator);

            IDictionary<string, object> responseProperties = await TestAmqpHost.ProcessCbsRequestAsync(testMessageId, processor);

            responseProperties.ShouldBe(new Dictionary<string, object>
            {
                ["CorrelationId"] = testMessageId,
                ["status-code"] = 401
            });
        }

        [Test]
        public async Task Process_LogSuccess_WhenTokenValid()
        {
            ILogger fakeLogger = Substitute.For<ILogger>();
            ILoggerProvider fakeLoggerProvider = Substitute.For<ILoggerProvider>();
            fakeLoggerProvider.CreateLogger(Arg.Any<string>()).Returns(fakeLogger);
            var processor = new CbsRequestProcessor(Substitute.For<ISecurityContext>(), fakeLoggerProvider, Substitute.For<ITokenValidator>());

            await TestAmqpHost.ProcessCbsRequestAsync("111", processor);

            fakeLogger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<FormattedLogValues>(f => f.ToString().StartsWith("Valid $cbs request")),
                Arg.Any<Exception>(),
                Arg.Any<Func<FormattedLogValues, Exception, string>>()
            );
        }

        [Test]
        public async Task Process_LogError_WhenTokenInvalid()
        {
            var testException = new ArgumentException("Test");
            ILogger fakeLogger = Substitute.For<ILogger>();
            ILoggerProvider fakeLoggerProvider = Substitute.For<ILoggerProvider>();
            fakeLoggerProvider.CreateLogger(Arg.Any<string>()).Returns(fakeLogger);
            ITokenValidator fakeTokenValidator = Substitute.For<ITokenValidator>();
            fakeTokenValidator
                .When(instance => instance.Validate(Arg.Any<string>()))
                .Do(_ => throw testException);
            var processor = new CbsRequestProcessor(Substitute.For<ISecurityContext>(), fakeLoggerProvider, fakeTokenValidator);

            await TestAmqpHost.ProcessCbsRequestAsync("abc", processor);

            fakeLogger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<FormattedLogValues>(f => f.ToString().StartsWith("Failed to validate $cbs request")),
                testException,
                Arg.Any<Func<FormattedLogValues, Exception, string>>()
            );
        }
    }
}
