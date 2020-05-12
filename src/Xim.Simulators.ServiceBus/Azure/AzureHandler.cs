using System;
using Amqp.Handler;

namespace Xim.Simulators.ServiceBus.Azure
{
    internal sealed class AzureHandler : IHandler
    {
        public static AzureHandler Instance { get; } = new AzureHandler();

        private AzureHandler() { }

        public bool CanHandle(EventId id)
            => id == EventId.SendDelivery;

        public void Handle(Event protocolEvent)
        {
            if (protocolEvent.Id == EventId.SendDelivery && protocolEvent.Context is IDelivery delivery)
            {
                HandleSendDelivery(delivery);
            }
        }

        private static void HandleSendDelivery(IDelivery delivery)
            => delivery.Tag = Guid.NewGuid().ToByteArray();
    }
}
