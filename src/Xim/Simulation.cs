using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xim.Simulators;

namespace Xim
{
    /// <summary>
    /// Simulation that can manage simulators.
    /// </summary>
    public sealed class Simulation : ISimulation, IAddSimulator
    {
        private bool _disposed;
        private readonly List<ISimulator> _simulators = new List<ISimulator>();

        /// <summary>
        /// Gets an enumerable of simulators attached to the simulation.
        /// </summary>
        public IEnumerable<ISimulator> Simulators => GetSimulators();

        private Simulation() { }

        private IEnumerable<ISimulator> GetSimulators()
        {
            foreach (ISimulator simulator in _simulators)
                yield return simulator;
        }

        TSimulator IAddSimulator.Add<TSimulator>(TSimulator simulator)
        {
            _simulators.Add(simulator ?? throw new ArgumentNullException(nameof(simulator)));
            return simulator;
        }

        /// <summary>
        /// Stops all attached simulators asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public Task StopAllAsync()
            => Task.WhenAll(_simulators.Select(simulator => simulator.StopAsync()));

        /// <summary>
        /// Creates a new simulation.
        /// </summary>
        /// <returns>The new simulation instance.</returns>
        public static ISimulation Create()
            => new Simulation();

        /// <summary>
        /// Disposes managed resources associated with the simulation.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (IDisposable simulator in _simulators.OfType<IDisposable>())
            {
                simulator.Dispose();
            }

            _disposed = true;
        }
    }
}
