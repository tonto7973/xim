using System.Collections.Generic;
using Xim.Simulators.ServiceBus.Entities;

namespace Xim.Simulators.ServiceBus.Processing
{
    internal interface IEntityLookup : IEnumerable<(string Address, IEntity Entity)>
    {
        IEntity Find(string name);
    }
}
