using System.Threading.Tasks;

namespace Xim.Simulators
{
    /// <summary>
    /// Represents a simulator.
    /// </summary>
    public interface ISimulator
    {
        /// <summary>
        /// Gets the current simulator state.
        /// </summary>
        SimulatorState State { get; }

        /// <summary>
        /// Starts the simulator.
        /// </summary>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        Task StartAsync();

        /// <summary>
        /// Gracefully stops the simulator.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        Task StopAsync();

        /// <summary>
        /// Aborts the simulator.
        /// </summary>
        void Abort();
    }
}
