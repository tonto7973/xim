using System;
using System.Collections.Generic;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Represents an api simulator.
    /// </summary>
    public interface IApiSimulator : ISimulator, IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ApiSimulatorSettings"/> for the api simulator.
        /// </summary>
        ApiSimulatorSettings Settings { get; }

        /// <summary>
        /// Gets the port the api simulator is running on.
        /// </summary>
        /// <exception cref="InvalidOperationException">If simulator not running.</exception>
        int Port { get; }

        /// <summary>
        /// Gets the URL of the api simulator.
        /// </summary>
        /// <exception cref="InvalidOperationException">If simulator not running.</exception>
        string Location { get; }

        /// <summary>
        /// Gets the enumerable of received api calls for this simulator.
        /// </summary>
        IReadOnlyList<ApiCall> ReceivedApiCalls { get; }
    }
}
