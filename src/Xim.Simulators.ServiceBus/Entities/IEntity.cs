using Xim.Simulators.ServiceBus.Delivering;

namespace Xim.Simulators.ServiceBus.Entities
{
    internal interface IEntity
    {
        string Name { get; }

        DeliveryQueue DeliveryQueue { get; }

        void Post(Amqp.Message message);
    }
}
