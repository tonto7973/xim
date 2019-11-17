using System.Threading.Tasks;
using Xim.Simulators;

namespace Xim.Tests
{
    public class FakeSimulator : ISimulator
    {
        public SimulatorState State => SimulatorState.Stopped;

        public Task StartAsync() => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;
    }
}
