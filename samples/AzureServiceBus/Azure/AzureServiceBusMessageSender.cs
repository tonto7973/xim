using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace AzureServiceBusSample.Azure
{
    public class AzureServiceBusMessageSender
    {
        private readonly MessageSender _sender;

        public AzureServiceBusMessageSender(AzureServiceBusSettings settings)
        {
            _sender = new MessageSender(settings.OutConnectionString, settings.OutQueueName);
        }

        public Task SendAsync(string payload, Guid correlationId)
        {
            var message = new Message
            {
                Body = Encoding.UTF8.GetBytes(payload),
                CorrelationId = correlationId.ToString()
            };
            return _sender.SendAsync(message);
        }
    }
}
