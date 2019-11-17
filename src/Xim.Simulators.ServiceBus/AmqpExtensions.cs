using Amqp;
using Amqp.Listener;
using Amqp.Types;
using Microsoft.Extensions.Logging;
using Xim.Simulators.ServiceBus.Azure;
using Xim.Simulators.ServiceBus.Processing;

namespace Xim.Simulators.ServiceBus
{
    internal static class AmqpExtensions
    {
        private static readonly Symbol XOptSequenceNumber = "x-opt-sequence-number";

        internal static Message Clone(this Message message)
            => message == null
                ? null
                : Message.Decode(message.Encode());

        internal static Message AddSequenceNumber(this Message message, long sequence)
        {
            if (message != null)
            {
                if (message.MessageAnnotations == null)
                    message.MessageAnnotations = new Amqp.Framing.MessageAnnotations();
                if (message.MessageAnnotations[XOptSequenceNumber] == null)
                    message.MessageAnnotations[XOptSequenceNumber] = sequence;
            }
            return message;
        }

        internal static void RegisterManagementProcessors(this IContainerHost host, IEntityLookup entityLookup, ILoggerProvider loggerProvider)
        {
            foreach (var item in entityLookup)
            {
                if (item.Entity.DeliveryQueue != null)
                {
                    host.RegisterRequestProcessor(item.Address + "/$management", new ManagementRequestProcessor(loggerProvider));
                }
            }
        }

        public static void EnableAzureSaslMechanism(this ConnectionListener.SaslSettings sasl)
        {
            var profile = new AzureSaslProfile();
            sasl.EnableMechanism(profile.Mechanism, profile);
        }
    }
}
