using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators.ServiceBus.Delivering;
using Xim.Simulators.ServiceBus.Entities;

namespace Xim.Simulators.ServiceBus.Tests
{
    [TestFixture]
    public class XimSbSimulatorSmokeTests
    {
        [Test]
        public async Task TestAzureServiceBusQueueReceive()
        {
            X509Certificate2 testCertificate = TestCertificate.Find();
            if (testCertificate == null)
            {
                Assert.Inconclusive("The test SSL certificate is not available.");
            }

            using ISimulation simulation = Simulation.Create();
            const string queueName = "sb-queue-x";
            Exception busException = null;
            IDelivery[] deliveries = null;

            IServiceBusSimulator bus = simulation.AddServiceBus()
                .SetCertificate(testCertificate)
                .AddQueue(queueName)
                .Build();

            await bus.StartAsync();
            IQueue busQueue = bus.Queues[queueName];

            var receiver = new MessageReceiver(bus.ConnectionString, queueName);
            try
            {
                receiver.ServiceBusConnection.OperationTimeout = TimeSpan.FromHours(1);
                receiver.RegisterMessageHandler(async (message, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (message.MessageId == "2481")
                        await receiver.DeadLetterAsync(message.SystemProperties.LockToken);
                    else if (message.MessageId == "51847")
                        await receiver.AbandonAsync(message.SystemProperties.LockToken);
                    else
                        await receiver.CompleteAsync(message.SystemProperties.LockToken);
                }, new MessageHandlerOptions(e =>
                {
                    if (!(e.Exception is OperationCanceledException))
                        busException = e.Exception;
                    return Task.CompletedTask;
                })
                {
                    AutoComplete = false
                });

                busQueue.Post(new Amqp.Message
                {
                    Properties = new Amqp.Framing.Properties { MessageId = "2481" }
                });
                busQueue.Post(new Amqp.Message
                {
                    Properties = new Amqp.Framing.Properties { MessageId = "51847" },
                });
                busQueue.Post(new Amqp.Message
                {
                    Properties = new Amqp.Framing.Properties { MessageId = "33782" },
                });
                busQueue.Post(new Amqp.Message());
                deliveries = busQueue.Deliveries.ToArray();

                IEnumerable<Task<bool>> deliveryTasks = deliveries.Select(d => d.WaitAsync(TimeSpan.FromSeconds(5)));
                await Task.WhenAll(deliveryTasks);
            }
            finally
            {
                await receiver.CloseAsync();
                await bus.StopAsync();
            }

            bus.ShouldSatisfyAllConditions(
                () => deliveries.Length.ShouldBe(4),
                () => deliveries[0].Result.ShouldBe(DeliveryResult.DeadLettered),
                () => deliveries[1].Result.ShouldBe(DeliveryResult.Abandoned),
                () => deliveries[2].Result.ShouldBe(DeliveryResult.Completed),
                () => deliveries[3].Result.ShouldBe(DeliveryResult.Completed)
            );
        }

        [Test]
        public async Task TestAzureServiceBusQueueSend()
        {
            X509Certificate2 testCertificate = TestCertificate.Find();
            if (testCertificate == null)
            {
                Assert.Inconclusive("The test SSL certificate is not available.");
            }

            using ISimulation simulation = Simulation.Create();
            const string queueName = "my-queue-3278";
            var messageBody = Encoding.ASCII.GetBytes("DTESTa");

            IServiceBusSimulator bus = simulation.AddServiceBus()
                .SetCertificate(testCertificate)
                .AddQueue(queueName)
                .Build();

            await bus.StartAsync();

            var queueClient = new QueueClient(bus.ConnectionString, queueName);

            var message = new Message(messageBody) { CorrelationId = "3278" };
            message.UserProperties["BoldManTrue"] = 32;
            await queueClient.SendAsync(message);
            await queueClient.CloseAsync();

            await bus.StopAsync();

            IReadOnlyList<IDelivery> deliveries = bus.Queues[queueName].Deliveries;

            bus.ShouldSatisfyAllConditions(
                () => deliveries.Count.ShouldBe(1),
                () => deliveries[0].Message.Body.ShouldBe(messageBody),
                () => deliveries[0].Message.ApplicationProperties["BoldManTrue"].ShouldBe(32)
            );
        }
    }
}
