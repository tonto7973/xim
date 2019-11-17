using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xim.Simulators;

namespace Xim
{
    /// <summary>
    /// Provides a mechanism to manage simulators.
    /// </summary>
    public interface ISimulation : IDisposable
    {
        /// <summary>
        /// Gets an enumerable that can iterate through simulators attached to the simulation.
        /// </summary>
        IEnumerable<ISimulator> Simulators { get; }

        /// <summary>
        /// Stops all attached simulators asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        Task StopAllAsync();
    }
}
