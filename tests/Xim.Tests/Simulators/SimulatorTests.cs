using System;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Tests
{
    [TestFixture]
    public class SimulatorTests
    {
        [TestCase(SimulatorState.Stopped, SimulatorState.Running)]
        [TestCase(SimulatorState.Stopped, SimulatorState.Stopping)]
        [TestCase(SimulatorState.Starting, SimulatorState.Stopping)]
        [TestCase(SimulatorState.Running, SimulatorState.Starting)]
        [TestCase(SimulatorState.Running, SimulatorState.Stopped)]
        [TestCase(SimulatorState.Stopping, SimulatorState.Starting)]
        [TestCase(SimulatorState.Stopping, SimulatorState.Running)]
        public void SetState_Throws_WhenStateCannotTransition(SimulatorState from, SimulatorState to)
        {
            TestSimulator simulator = Substitute.ForPartsOf<TestSimulator>();

            while (simulator.State != from)
            {
                simulator.SetState(simulator.State + 1);
            }

            Action action = () => simulator.SetState(to);

            action.ShouldThrow<InvalidOperationException>()
                .Message.ShouldBe(SR.Format(SR.SimulatorStateCannotTransition, from, to));
        }

        [TestCase(SimulatorState.Stopped, SimulatorState.Starting)]
        [TestCase(SimulatorState.Starting, SimulatorState.Stopped)]
        [TestCase(SimulatorState.Starting, SimulatorState.Running)]
        [TestCase(SimulatorState.Running, SimulatorState.Stopping)]
        [TestCase(SimulatorState.Stopping, SimulatorState.Stopped)]
        public void SetState_DoesNotThrow_WhenStateCanTransition(SimulatorState from, SimulatorState to)
        {
            TestSimulator simulator = Substitute.ForPartsOf<TestSimulator>();

            while (simulator.State != from)
            {
                simulator.SetState(simulator.State + 1);
            }

            Action action = () => simulator.SetState(to);

            action.ShouldNotThrow();
        }

        [TestCase(SimulatorState.Running)]
        [TestCase(SimulatorState.Stopping)]
        public void SetState_DoesNotThrow_WhenStateDoesNotChange(SimulatorState state)
        {
            TestSimulator simulator = Substitute.ForPartsOf<TestSimulator>();

            while (simulator.State != state)
            {
                simulator.SetState(simulator.State + 1);
            }

            Action action = () => simulator.SetState(state);

            action.ShouldNotThrow();
        }

        [TestCase(SimulatorState.Stopped, SimulatorState.Running)]
        [TestCase(SimulatorState.Stopped, SimulatorState.Stopping)]
        [TestCase(SimulatorState.Starting, SimulatorState.Stopping)]
        [TestCase(SimulatorState.Running, SimulatorState.Starting)]
        [TestCase(SimulatorState.Running, SimulatorState.Stopped)]
        [TestCase(SimulatorState.Stopping, SimulatorState.Starting)]
        [TestCase(SimulatorState.Stopping, SimulatorState.Running)]
        public void TrySetState_ReturnsFalse_WhenStateCannotTransition(SimulatorState from, SimulatorState to)
        {
            TestSimulator simulator = Substitute.ForPartsOf<TestSimulator>();

            while (simulator.State != from)
            {
                simulator.SetState(simulator.State + 1);
            }

            var result = simulator.TrySetState(to);

            result.ShouldBeFalse();
        }

        [TestCase(SimulatorState.Stopped, SimulatorState.Starting)]
        [TestCase(SimulatorState.Starting, SimulatorState.Stopped)]
        [TestCase(SimulatorState.Starting, SimulatorState.Running)]
        [TestCase(SimulatorState.Running, SimulatorState.Stopping)]
        [TestCase(SimulatorState.Stopping, SimulatorState.Stopped)]
        public void TrySetState_ReturnsTrue_WhenStateCannotTransition(SimulatorState from, SimulatorState to)
        {
            TestSimulator simulator = Substitute.ForPartsOf<TestSimulator>();

            while (simulator.State != from)
            {
                simulator.SetState(simulator.State + 1);
            }

            var result = simulator.TrySetState(to);

            result.ShouldBeTrue();
        }

        [TestCase(SimulatorState.Stopped)]
        [TestCase(SimulatorState.Starting)]
        public void TrySetState_ReturnsTrue_WhenStateDoesNotChange(SimulatorState state)
        {
            TestSimulator simulator = Substitute.ForPartsOf<TestSimulator>();

            while (simulator.State != state)
            {
                simulator.SetState(simulator.State + 1);
            }

            var result = simulator.TrySetState(state);

            result.ShouldBeTrue();
        }
    }

    public abstract class TestSimulator : Simulator
    {
        public new bool TrySetState(SimulatorState state)
            => base.TrySetState(state);

        public new void SetState(SimulatorState state)
            => base.SetState(state);
    }
}
