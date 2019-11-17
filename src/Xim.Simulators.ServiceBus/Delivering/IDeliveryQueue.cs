﻿using System.Threading;
using Amqp;
using Amqp.Listener;

namespace Xim.Simulators.ServiceBus.Delivering
{
    internal interface IDeliveryQueue
    {
        void Enqueue(Delivery delivery);

        Message Dequeue(CancellationToken cancellationToken);

        void Process(MessageContext messageContext);
    }
}
