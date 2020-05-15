using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Sasl;
using NSubstitute;
using Xim.Simulators.ServiceBus.Processing;

namespace Xim.Simulators.ServiceBus.Tests
{
    internal static class TestAmqpHost
    {
        public static ContainerHost Open(int preferredPort = 0)
        {
            var port = preferredPort == 0 ? TestTcpUtils.FindFreePort() : preferredPort;
            var address = new Address($"amqp://localhost:{port}");
            var host = new ContainerHost(address);
            host.Listeners[0].SASL.EnableExternalMechanism = true;
            host.Listeners[0].SASL.EnableAnonymousMechanism = true;
            host.Open();
            return host;
        }

        public static async Task<Connection> ConnectAsync(this ContainerHost host)
        {
            var factory = new ConnectionFactory();
            factory.SASL.Profile = SaslProfile.Anonymous;
            var address = new Address("localhost", host.Listeners[0].Address.Port, null, null, "/", "amqp");
            return await factory.CreateAsync(address);
        }

        public static async Task<Connection> ConnectAndAttachAsync(this ContainerHost host, int nSessions = 1)
        {
            var connection = await host.ConnectAsync();
            try
            {
                var links = Enumerable
                    .Range(1, nSessions)
                    .Select(i => new SenderLink(new Session(connection), "A" + i, "A" + i));

                Parallel.ForEach(links, link => link.Send(new Message("msg1"), TimeSpan.FromMilliseconds(500)));
            }
            catch
            {
                // ignore
            }
            return connection;
        }

        public static async Task<Message> SendControlRequestAsync(this Session session, string controller, Message request)
        {
            if (request.Properties == null)
                request.Properties = new Properties();
            request.Properties.ReplyTo = "c-client-reply-to";
            var cbsSender = new SenderLink(session, "c-sender", controller);
            var cbsReceiver = new ReceiverLink(session, "c-receiver", new Attach
            {
                Source = new Source { Address = controller },
                Target = new Target { Address = "c-client-reply-to" }
            }, null);
            try
            {
                cbsReceiver.SetCredit(200, true);
                await cbsSender.SendAsync(request);
                return await cbsReceiver.ReceiveAsync();
            }
            finally
            {
                try
                {
                    try
                    {
                        await cbsSender.CloseAsync();
                    }
                    finally
                    {
                        await cbsReceiver.CloseAsync();
                    }
                }
                catch (AmqpException)
                {
                    // ignore for closeasync
                }
            }
        }

        public static Task<Message> SendCbsRequestAsync(this Session session, string messageId)
        {
            var request = new Message("SharedAccessSignature sr=http%3a%2f%2flocalhost%2fbaonline-topic%2fSubscriptions%2fbaonline-topic-sub%2f&sig=mPgB3dbPPhvPYkgau2sDSFDqB5t1pskqqRyuiIA%2f2IM%3d&se=1546455437&skn=all")
            {
                Properties = new Properties
                {
                    MessageId = messageId
                },
                ApplicationProperties = new ApplicationProperties
                {
                    ["operation"] = "put-token",
                    ["type"] = "servicebus.windows.net:sastoken",
                    ["name"] = "amqp://localhost/baonline-topic/Subscriptions/baonline-topic-sub"
                }
            };
            return session.SendControlRequestAsync("$cbs", request);
        }

        public static async Task<IDictionary<string, object>> ProcessCbsRequestAsync(string messageId, CbsRequestProcessor processor)
        {
            var responseProperties = new Dictionary<string, object>();
            var host = Open();
            try
            {
                host.RegisterRequestProcessor("$cbs", processor);
                var connection = await host.ConnectAsync();
                var session = new Session(connection);
                try
                {
                    var response = await session.SendCbsRequestAsync(messageId);
                    responseProperties["CorrelationId"] = response.Properties.CorrelationId;
                    responseProperties["status-code"] = response.ApplicationProperties["status-code"];
                }
                finally
                {
                    await session.CloseAsync();
                    await connection.CloseAsync();
                }
            }
            finally
            {
                host.Close();
            }
            return responseProperties;
        }

        public static async Task<Message> ProcessManagementRequestAsync(Message message, ManagementRequestProcessor processor)
        {
            var host = Open();
            try
            {
                host.RegisterRequestProcessor("$management", processor);
                var connection = await host.ConnectAsync();
                var session = new Session(connection);
                try
                {
                    return await session.SendControlRequestAsync("$management", message);
                }
                finally
                {
                    await session.CloseAsync();
                    await connection.CloseAsync();
                }
            }
            finally
            {
                host.Close();
            }
        }

        public static async Task<Session> OpenAndLinkProcessorAsync(ILinkProcessor linkProcessor)
        {
            var host = Open();
            host.RegisterLinkProcessor(linkProcessor);
            var connection = await host.ConnectAsync();
            var session = new Session(connection);
            session.AddClosedCallback((_, __) => host.Close());
            return session;
        }

        public static Task<Session> OpenAndLinkEndpointAsync(LinkEndpoint endpoint)
        {
            var fakeLinkProcessor = Substitute.For<ILinkProcessor>();
            fakeLinkProcessor
                .When(instance => instance.Process(Arg.Any<AttachContext>()))
                .Do(call => call.Arg<AttachContext>().Complete(endpoint, 3));

            return OpenAndLinkProcessorAsync(fakeLinkProcessor);
        }

        public static async Task<ReceiverLink> OpenAndLinkReceiverAsync(LinkEndpoint endpoint)
        {
            var session = await OpenAndLinkEndpointAsync(endpoint);
            return new ReceiverLink(session, "any", "abc/def");
        }
    }
}
