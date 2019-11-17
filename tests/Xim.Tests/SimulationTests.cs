using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using Xim.Simulators;

namespace Xim.Tests
{
    [TestFixture]
    public class SimulationTests
    {
        [Test]
        public void Create_CreatesNewInstance()
        {
            var simulation = Simulation.Create();

            simulation.ShouldNotBeNull();
        }

        [Test]
        public void Add_Throws_WhenSimulatorNull()
        {
            var simulation = (IAddSimulator)Simulation.Create();

            Action action = () => simulation.Add((FakeSimulator)null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("simulator");
        }

        [Test]
        public void Add_AddsSimulator()
        {
            var simulation = Simulation.Create();
            var simulator = Substitute.For<ISimulator>();

            ((IAddSimulator)simulation).Add(simulator);

            simulation.Simulators.Single().ShouldBeSameAs(simulator);
        }

        [Test]
        public async Task StopAll_StopsAllSimulators()
        {
            var simulation = Simulation.Create();
            var simulator1 = Substitute.For<ISimulator>();
            var simulator2 = Substitute.For<ISimulator>();
            ((IAddSimulator)simulation).Add(simulator1);
            ((IAddSimulator)simulation).Add(simulator2);

            await simulation.StopAllAsync();

            simulation.ShouldSatisfyAllConditions(
                () => simulator1.Received(1).StopAsync(),
                () => simulator2.Received(1).StopAsync()
            );
        }

        [Test]
        public void Dispose_DisposesSimulator_WhenSimulatorImplementsIDisposable()
        {
            object simulator = Substitute.For<ISimulator, IDisposable>();

            using (var simulation = Simulation.Create())
            {
                ((IAddSimulator)simulation).Add((ISimulator)simulator);
            }

            ((IDisposable)simulator).Received(1).Dispose();
        }

        [Test]
        public void Dispose_DoesNotThrow_WhenSimulatorDoesNotImplementIDisposable()
        {
            var simulator = Substitute.For<ISimulator>();
            var simulation = Simulation.Create();
            ((IAddSimulator)simulation).Add(simulator);

            Action action = () => simulation.Dispose();

            action.ShouldNotThrow();
        }

        [Test]
        public void Dispose_DoesNotThrow_WhenSimulatorThrowsWhenStopping()
        {
            var simulator = Substitute.For<ISimulator>();
            simulator.StopAsync().Returns(_ => Task.FromException(new ArgumentException("Task is null")));
            var simulation = Simulation.Create();
            ((IAddSimulator)simulation).Add(simulator);

            Action action = () => simulation.Dispose();

            action.ShouldNotThrow();
        }

        [Test]
        public void Dispose_DisposesOnlyOnce()
        {
            object simulator = Substitute.For<ISimulator, IDisposable>();
            var simulation = Simulation.Create();
            ((IAddSimulator)simulation).Add((ISimulator)simulator);

#pragma warning disable S3966 // Objects should not be disposed more than once - required for unit test
            simulation.Dispose();
            simulation.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once

            ((IDisposable)simulator).Received(1).Dispose();
        }
    }
}