using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureServiceBusSample.Azure;
using NUnit.Framework;
using Xim;
using Xim.Simulators.ServiceBus;
using Xim.Simulators.ServiceBus.Delivering;

namespace AzureServiceBusSample.Tests
{
    public class AzureServiceBusTests
    {
        private const string TestInQueue = "sb-test-in-queue";
        private const string TestOutQueue = "sb-test-out-queue";

        private ISimulation _simulation;
        private X509Certificate2 _testCertificate;

        [SetUp]
        public void SetUp()
        {
            _simulation = Simulation.Create();
            _testCertificate = TestCertificate.Find();
            if (_testCertificate == null)
                Assert.Inconclusive("The test SSL certificate is not available. Use tools/Xim.Tests.Setup to install the certificate.");
        }

        [TearDown]
        public async Task TearDown()
        {
            await _simulation.StopAllAsync();
            _simulation.Dispose();
            _testCertificate?.Dispose();
        }

        [Test]
        public async Task Receiver_CompletesDelivery_WhenBodyIsValid()
        {
            IDelivery delivery;
            var simulator = _simulation.AddServiceBus()
                .SetCertificate(_testCertificate)
                .AddQueue(TestInQueue)
                .Build();
            await simulator
                .StartAsync()
                .ConfigureAwait(false);
            var settings = new AzureServiceBusSettings
            {
                InConnectionString = simulator.ConnectionString,
                InQueueName = TestInQueue
            };
            var receiver = new AzureServiceBusMessageReceiver(settings, new AzureServiceBusMessageHandler());

            using var cts = new CancellationTokenSource();
            var runTask = receiver.RunAsync(cts.Token);
            delivery = simulator.Queues[TestInQueue].Post(new Amqp.Message()
            {
                BodySection = new Amqp.Framing.Data
                {
                    Binary = Encoding.ASCII.GetBytes("body1abc")
                },
                Properties = new Amqp.Framing.Properties { MessageId = "8457986" }
            });
            var deliveryTask = delivery.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.WhenAny(runTask, deliveryTask).ConfigureAwait(false);
            cts.Cancel();

            Assert.AreEqual(DeliveryResult.Completed, delivery.Result);
        }

        [Test]
        public async Task Receiver_AbandonsDelivery_WhenBodyIsInvalid()
        {
            IDelivery delivery;
            var simulator = _simulation.AddServiceBus()
                .SetCertificate(_testCertificate)
                .AddQueue(TestInQueue)
                .Build();
            await simulator
                .StartAsync()
                .ConfigureAwait(false);
            var settings = new AzureServiceBusSettings
            {
                InConnectionString = simulator.ConnectionString,
                InQueueName = TestInQueue
            };
            var receiver = new AzureServiceBusMessageReceiver(settings, new AzureServiceBusMessageHandler());

            using var cts = new CancellationTokenSource();
            var runTask = receiver.RunAsync(cts.Token);
            delivery = simulator.Queues[TestInQueue].Post(new Amqp.Message()
            {
                BodySection = new Amqp.Framing.Data
                {
                    Binary = Encoding.ASCII.GetBytes("body2")
                },
                Properties = new Amqp.Framing.Properties { MessageId = "54795232" }
            });
            var deliveryTask = delivery.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.WhenAny(runTask, deliveryTask).ConfigureAwait(false);
            cts.Cancel();

            Assert.AreEqual(DeliveryResult.Abandoned, delivery.Result);
        }

        [Test]
        public async Task Sender_SendsMessage()
        {
            var correlationId = Guid.NewGuid();
            var simulator = _simulation.AddServiceBus()
                .SetCertificate(_testCertificate)
                .AddTopic(TestOutQueue, new[] { "sub-a", "sub-b" })
                .Build();
            await simulator
                .StartAsync()
                .ConfigureAwait(false);

            var settings = new AzureServiceBusSettings
            {
                OutConnectionString = simulator.ConnectionString,
                OutQueueName = TestOutQueue
            };
            var sender = new AzureServiceBusMessageSender(settings);
            await sender.SendAsync("Ehlo", correlationId);

            var delivery = simulator.Topics[TestOutQueue].Deliveries[0];
            var body = delivery.Message.BodySection as Amqp.Framing.Data;

            Assert.AreEqual(correlationId.ToString(), delivery.Message.Properties.CorrelationId);
            Assert.AreEqual("Ehlo", Encoding.UTF8.GetString(body.Binary));
        }
    }
}