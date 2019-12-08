using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xim.Simulators
{
    /// <summary>
    /// Abstract class that provides simulator state handling.
    /// </summary>
    public abstract class Simulator : ISimulator
    {
        private static readonly IDictionary<SimulatorState, SimulatorState[]> AllowedStateTransitions =
            new Dictionary<SimulatorState, SimulatorState[]>
            {
                [SimulatorState.Stopped] = new[] { SimulatorState.Starting },
                [SimulatorState.Starting] = new[] { SimulatorState.Running, SimulatorState.Stopped },
                [SimulatorState.Running] = new[] { SimulatorState.Stopping },
                [SimulatorState.Stopping] = new[] { SimulatorState.Stopped }
            };

        private readonly object _stateLock = new object();

        /// <summary>
        /// Gets the current simulator state.
        /// </summary>
        public SimulatorState State { get; private set; }

        /// <summary>
        /// Starts the simulator.
        /// </summary>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        public abstract Task StartAsync();

        /// <summary>
        /// Gracefully stops the simulator.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public abstract Task StopAsync();

        /// <summary>
        /// Aborts the simulator.
        /// </summary>
        public abstract void Abort();

        /// <summary>
        /// Sets the simulator state.
        /// </summary>
        /// <param name="state">Simulator state.</param>
        /// <exception cref="InvalidOperationException">If state cannot be set.</exception>
        protected void SetState(SimulatorState state)
        {
            if (!TrySetState(state))
                throw new InvalidOperationException(SR.Format(SR.SimulatorStateCannotTransition, State, state));
        }

        /// <summary>
        /// Set the simulator state. A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="state">The new simulator state.</param>
        /// <returns>True if the new state was set; false otherwise.</returns>
        protected bool TrySetState(SimulatorState state)
        {
            bool canTransition;

            lock (_stateLock)
            {
                canTransition = State == state || AllowedStateTransitions[State].Contains(state);
                if (canTransition && State != state)
                    State = state;
            }

            return canTransition;
        }
    }
}
