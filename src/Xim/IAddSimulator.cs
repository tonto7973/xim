using Xim.Simulators;

namespace Xim
{
    internal interface IAddSimulator
    {
        TSimulator Add<TSimulator>(TSimulator simulator) where TSimulator : class, ISimulator;
    }
}
