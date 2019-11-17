using System;
using System.Text;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Delivering;
using Xim.Simulators.ServiceBus.Tests;

namespace Xim.Simulators.ServiceBus.Processing.Tests
{
    [TestFixture]
    public class LinkProcessorTests
    {
        [Test]
        public async Task Process_CompleteWithError_WhenLinkNameEmpty()
        {
            const string emptyLinkName = "";
            const string entity = "entity";
            var securityContext = Substitute.For<ISecurityContext>();
            var entityLookup = Substitute.For<IEntityLookup>();
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var linkProcessor = new LinkProcessor(securityContext, entityLookup, loggerProvider);
            AmqpException exception = null;
            var session = await TestAmqpHost.OpenAndLinkProcessorAsync(linkProcessor);
            try
            {
                var sender = new SenderLink(session, emptyLinkName, entity);
                var message = new Message
                {
                    Properties = new Properties { MessageId = "message1" },
                    BodySection = new Data { Binary = Encoding.UTF8.GetBytes("hello!") }
                };
                Func<Task> action = async () => await sender.SendAsync(message);

                linkProcessor.ShouldSatisfyAllConditions(
                    () => exception = action.ShouldThrow<AmqpException>(),
                    () => exception.Error.Condition.ShouldBe((Symbol)ErrorCode.InvalidField),
                    () => exception.Error.Description.ShouldBe("Empty link name not allowed.")
                );
            }
            finally
            {
                await session.Connection.CloseAsync();
            }
        }

        [Test]
        public async Task Process_CompleteWithError_WhenConnectionNotAuthorized()
        {
            const string linkName = "abcd";
            const string entity = "entity";
            var securityContext = Substitute.For<ISecurityContext>();
            var entityLookup = Substitute.For<IEntityLookup>();
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var linkProcessor = new LinkProcessor(securityContext, entityLookup, loggerProvider);
            securityContext.IsAuthorized(Arg.Any<Connection>()).Returns(false);
            AmqpException exception = null;
            var session = await TestAmqpHost.OpenAndLinkProcessorAsync(linkProcessor);
            try
            {
                var sender = new SenderLink(session, linkName, entity);
                var message = new Message
                {
                    Properties = new Properties { MessageId = "message1" },
                    BodySection = new Data { Binary = Encoding.UTF8.GetBytes("hello!") }
                };
                Func<Task> action = async () => await sender.SendAsync(message);

                linkProcessor.ShouldSatisfyAllConditions(
                    () => exception = action.ShouldThrow<AmqpException>(),
                    () => exception.Error.Condition.ShouldBe((Symbol)ErrorCode.UnauthorizedAccess),
                    () => exception.Error.Description.ShouldBe("Not authorized.")
                );
            }
            finally
            {
                await session.Connection.CloseAsync();
            }
        }

        [Test]
        public async Task Process_CompleteWithError_WhenIncomingLinkEntityNotFound()
        {
            const string linkName = "abcd";
            const string entity = "entity";
            var securityContext = Substitute.For<ISecurityContext>();
            var entityLookup = Substitute.For<IEntityLookup>();
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var linkProcessor = new LinkProcessor(securityContext, entityLookup, loggerProvider);
            entityLookup.Find(Arg.Any<string>()).Returns((Entities.IEntity)null);
            securityContext.IsAuthorized(Arg.Any<Connection>()).Returns(true);
            AmqpException exception = null;
            var session = await TestAmqpHost.OpenAndLinkProcessorAsync(linkProcessor);
            try
            {
                var sender = new SenderLink(session, linkName, entity);
                var message = new Message
                {
                    Properties = new Properties { MessageId = "message1" },
                    BodySection = new Data { Binary = Encoding.UTF8.GetBytes("hello!") }
                };
                Func<Task> action = async () => await sender.SendAsync(message);

                linkProcessor.ShouldSatisfyAllConditions(
                    () => exception = action.ShouldThrow<AmqpException>(),
                    () => exception.Error.Condition.ShouldBe((Symbol)ErrorCode.NotFound),
                    () => exception.Error.Description.ShouldBe("Entity not found.")
                );
            }
            finally
            {
                await session.Connection.CloseAsync();
            }
        }

        [Test]
        public async Task Process_CompleteSuccessfully_WhenIncomingLinkEntityFound()
        {
            const string linkName = "abcd";
            const string entity = "myEntity";
            var securityContext = Substitute.For<ISecurityContext>();
            var entityLookup = Substitute.For<IEntityLookup>();
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var fakeEntity = Substitute.For<Entities.IEntity>();
            var linkProcessor = new LinkProcessor(securityContext, entityLookup, loggerProvider);
            entityLookup.Find(entity).Returns(fakeEntity);
            securityContext.IsAuthorized(Arg.Any<Connection>()).Returns(true);
            var session = await TestAmqpHost.OpenAndLinkProcessorAsync(linkProcessor);
            try
            {
                var sender = new SenderLink(session, linkName, entity);
                var message = new Message
                {
                    Properties = new Properties { MessageId = "message173" },
                    BodySection = new Data { Binary = Encoding.UTF8.GetBytes("hello!") }
                };

                await sender.SendAsync(message);

                fakeEntity
                    .Received(1)
                    .Post(Arg.Is<Message>(m => m.Properties.MessageId == "message173"));
            }
            finally
            {
                await session.Connection.CloseAsync();
            }
        }

        [Test]
        public async Task Process_CompleteWithError_WhenOutgoingLinkNotFound()
        {
            const string linkName = "abcd";
            const string entity = "entity";
            var securityContext = Substitute.For<ISecurityContext>();
            var entityLookup = Substitute.For<IEntityLookup>();
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var linkProcessor = new LinkProcessor(securityContext, entityLookup, loggerProvider);
            entityLookup.Find(Arg.Any<string>()).Returns((Entities.IEntity)null);
            securityContext.IsAuthorized(Arg.Any<Connection>()).Returns(true);
            AmqpException exception = null;
            var session = await TestAmqpHost.OpenAndLinkProcessorAsync(linkProcessor);
            try
            {
                var receiver = new ReceiverLink(session, linkName, entity);

                Func<Task> action = async () => await receiver.ReceiveAsync();

                linkProcessor.ShouldSatisfyAllConditions(
                    () => exception = action.ShouldThrow<AmqpException>(),
                    () => exception.Error.Condition.ShouldBe((Symbol)ErrorCode.NotFound),
                    () => exception.Error.Description.ShouldBe("Entity not found.")
                );
            }
            finally
            {
                await session.Connection.CloseAsync();
            }
        }

        [Test]
        public async Task Process_CompleteWithError_WhenOutgoingLinkQueueNotFound()
        {
            const string linkName = "abcd";
            const string entity = "entity";
            var securityContext = Substitute.For<ISecurityContext>();
            var entityLookup = Substitute.For<IEntityLookup>();
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var fakeEntity = Substitute.For<Entities.IEntity>();
            var linkProcessor = new LinkProcessor(securityContext, entityLookup, loggerProvider);
            fakeEntity.DeliveryQueue.Returns((DeliveryQueue)null);
            entityLookup.Find(Arg.Any<string>()).Returns(fakeEntity);
            securityContext.IsAuthorized(Arg.Any<Connection>()).Returns(true);
            AmqpException exception = null;
            var session = await TestAmqpHost.OpenAndLinkProcessorAsync(linkProcessor);
            try
            {
                var receiver = new ReceiverLink(session, linkName, entity);

                Func<Task> action = async () => await receiver.ReceiveAsync();

                linkProcessor.ShouldSatisfyAllConditions(
                    () => exception = action.ShouldThrow<AmqpException>(),
                    () => exception.Error.Condition.ShouldBe((Symbol)ErrorCode.NotFound),
                    () => exception.Error.Description.ShouldBe("Queue not found.")
                );
            }
            finally
            {
                await session.Connection.CloseAsync();
            }
        }

        [Test]
        public async Task Process_CompleteSuccessfully_WhenOutgoingLinkQueueFound()
        {
            const string linkName = "abcd";
            const string entity = "entity";
            var securityContext = Substitute.For<ISecurityContext>();
            var entityLookup = Substitute.For<IEntityLookup>();
            var loggerProvider = Substitute.For<ILoggerProvider>();
            var fakeEntity = Substitute.For<Entities.IEntity>();
            var deliveryQueue = new DeliveryQueue();
            var linkProcessor = new LinkProcessor(securityContext, entityLookup, loggerProvider);
            entityLookup.Find(Arg.Any<string>()).Returns(fakeEntity);
            securityContext.IsAuthorized(Arg.Any<Connection>()).Returns(true);
            fakeEntity.DeliveryQueue.Returns(deliveryQueue);
            var session = await TestAmqpHost.OpenAndLinkProcessorAsync(linkProcessor);
            try
            {
                var receiver = new ReceiverLink(session, linkName, entity);
                deliveryQueue.Enqueue(new Delivery(new Message { Properties = new Properties { MessageId = "msgid6746" } }));

                var message = await receiver.ReceiveAsync();

                message.Properties.MessageId.ShouldBe("msgid6746");
            }
            finally
            {
                await session.Connection.CloseAsync();
            }
        }
    }
}
