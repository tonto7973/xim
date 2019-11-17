using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Security.Tests
{
    [TestFixture]
    public class SecurityContextTests
    {
        [Test]
        public void Authorize_Throws_WhenConnectionNull()
        {
            var securityContext = new SecurityContext();

            Action action = () => securityContext.Authorize(null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("connection");
        }

        [Test]
        public void Authorize_DoesNotThrow_WhenConnectionIsNotListenerConnection()
        {
            var securityContext = new SecurityContext();
            var connection = Construct.Uninitialized<Connection>();

            Should.NotThrow(() => securityContext.Authorize(connection));
        }

        [Test]
        public async Task IsAuthorized_ReturnsTrue_WhenConnectionAuthorized()
        {
            ListenerLink link = null;
            var authorized = false;
            var fakeLinkProcessor = Substitute.For<ILinkProcessor>();
            fakeLinkProcessor
                .When(instance => instance.Process(Arg.Any<AttachContext>()))
                .Do(c =>
                {
                    var attachContext = c.ArgAt<AttachContext>(0);
                    link = attachContext.Link;
                    attachContext.Complete(new Error(ErrorCode.IllegalState) { Description = "Test" });
                });

            var host = TestAmqpHost.Open();
            try
            {
                host.RegisterLinkProcessor(fakeLinkProcessor);
                var connection = await host.ConnectAndAttachAsync();

                var securityContext = new SecurityContext();
                securityContext.Authorize(link.Session.Connection);
                authorized = securityContext.IsAuthorized(link.Session.Connection);

                await connection.CloseAsync();
            }
            finally
            {
                host.Close();
            }

            authorized.ShouldBeTrue();
        }

        [Test]
        public async Task IsAuthorized_ReturnsTrue_WhenSameConnectionAuthorizedTwice()
        {
            var authorized = false;
            var links = new List<ListenerLink>();
            var fakeLinkProcessor = Substitute.For<ILinkProcessor>();
            fakeLinkProcessor
                .When(instance => instance.Process(Arg.Any<AttachContext>()))
                .Do(c =>
                {
                    var attachContext = c.ArgAt<AttachContext>(0);
                    links.Add(attachContext.Link);
                    attachContext.Complete(new Error(ErrorCode.IllegalState) { Description = "Test" });
                });

            var host = TestAmqpHost.Open();
            try
            {
                host.RegisterLinkProcessor(fakeLinkProcessor);
                var connection = await host.ConnectAndAttachAsync(2);

                var securityContext = new SecurityContext();
                securityContext.Authorize(links[0].Session.Connection);
                securityContext.Authorize(links[1].Session.Connection);
                authorized = securityContext.IsAuthorized(links[1].Session.Connection);

                await connection.CloseAsync();
            }
            finally
            {
                host.Close();
            }

            authorized.ShouldBeTrue();
        }

        [Test]
        public async Task IsAuthorized_ReturnsFalse_WhenConnectionNotAuthorized()
        {
            ListenerLink link = null;
            var authorized = false;
            var fakeLinkProcessor = Substitute.For<ILinkProcessor>();
            fakeLinkProcessor
                .When(instance => instance.Process(Arg.Any<AttachContext>()))
                .Do(c =>
                {
                    var attachContext = c.ArgAt<AttachContext>(0);
                    link = attachContext.Link;
                    attachContext.Complete(new Error(ErrorCode.IllegalState) { Description = "Test" });
                });

            var host = TestAmqpHost.Open();
            try
            {
                host.RegisterLinkProcessor(fakeLinkProcessor);
                var connection = await host.ConnectAndAttachAsync();

                var securityContext = new SecurityContext();

                authorized = securityContext.IsAuthorized(link.Session.Connection);

                await connection.CloseAsync();
            }
            finally
            {
                host.Close();
            }

            authorized.ShouldBeFalse();
        }

        [Test]
        public async Task IsAuthorized_ReturnsFalse_WhenConnectionClosedAfterAuthorized()
        {
            ListenerLink link = null;
            var authorized = false;
            var fakeLinkProcessor = Substitute.For<ILinkProcessor>();
            fakeLinkProcessor
                .When(instance => instance.Process(Arg.Any<AttachContext>()))
                .Do(c =>
                {
                    var attachContext = c.ArgAt<AttachContext>(0);
                    link = attachContext.Link;
                    attachContext.Complete(new Error(ErrorCode.IllegalState) { Description = "Test" });
                });

            var securityContext = new SecurityContext();
            var host = TestAmqpHost.Open();
            try
            {
                host.RegisterLinkProcessor(fakeLinkProcessor);
                var connection = await host.ConnectAndAttachAsync();

                securityContext.Authorize(link.Session.Connection);
                await connection.CloseAsync();
            }
            finally
            {
                host.Close();
            }

            authorized = securityContext.IsAuthorized(link.Session.Connection);

            authorized.ShouldBeFalse();
        }

        [Test]
        public async Task IsAuthorized_ReturnsFalse_WhenSessionConnectionClosedBeforeAuthorized()
        {
            ListenerLink link = null;
            var authorized = false;
            var fakeLinkProcessor = Substitute.For<ILinkProcessor>();
            fakeLinkProcessor
                .When(instance => instance.Process(Arg.Any<AttachContext>()))
                .Do(c =>
                {
                    var attachContext = c.ArgAt<AttachContext>(0);
                    link = attachContext.Link;
                    attachContext.Complete(new Error(ErrorCode.IllegalState) { Description = "Test" });
                });

            var host = TestAmqpHost.Open();
            try
            {
                host.RegisterLinkProcessor(fakeLinkProcessor);
                var connection = await host.ConnectAndAttachAsync();
                await connection.CloseAsync();

                var securityContext = new SecurityContext();
                securityContext.Authorize(link.Session.Connection);

                authorized = securityContext.IsAuthorized(link.Session.Connection);
            }
            finally
            {
                host.Close();
            }

            authorized.ShouldBeFalse();
        }

        [Test]
        public void IsAuthorized_ReturnsFalse_WhenConnectionNotTheSame()
        {
            var connection1 = Construct.Uninitialized<ListenerConnection>();
            var connection2 = Construct.Uninitialized<Connection>();
            var securityContext = new SecurityContext();
            securityContext.Authorize(connection1);

            var result = securityContext.IsAuthorized(connection2);

            result.ShouldBeFalse();
        }

        [Test]
        public void IsAuthorized_ReturnsTrue_WhenConnectionTheSame()
        {
            var connection = Construct.Uninitialized<ListenerConnection>();
            var securityContext = new SecurityContext();
            securityContext.Authorize(connection);

            var result = securityContext.IsAuthorized(connection);

            result.ShouldBeTrue();
        }
    }
}
