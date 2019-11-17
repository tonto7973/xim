using System;
using System.Collections.Generic;
using Xim.Simulators.ServiceBus.Entities;

namespace Xim.Simulators.ServiceBus
{
    /// <summary>
    /// Represents a service bus simulator.
    /// </summary>
    public interface IServiceBusSimulator : ISimulator, IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ServiceBusSimulatorSettings"/> for the service bus simulator.
        /// </summary>
        ServiceBusSimulatorSettings Settings { get; }

        /// <summary>
        /// Gets the port the service bus simulator is running on.
        /// </summary>
        /// <exception cref="InvalidOperationException">If simulator not running.</exception>
        int Port { get; }

        /// <summary>
        /// Gets the URL of the service bus simulator.
        /// </summary>
        /// <exception cref="InvalidOperationException">If simulator not running.</exception>
        string Location { get; }

        /// <summary>
        /// Gets the connection string of the service bus simulator.
        /// </summary>
        /// <exception cref="InvalidOperationException">If simulator not running.</exception>
        string ConnectionString { get; }

        /// <summary>
        /// Gets the topics registered with the service bus simulator.
        /// </summary>
        IReadOnlyDictionary<string, ITopic> Topics { get; }

        /// <summary>
        /// Gets the queues registered with the service bus simulator.
        /// </summary>
        IReadOnlyDictionary<string, IQueue> Queues { get; }
    }
}
