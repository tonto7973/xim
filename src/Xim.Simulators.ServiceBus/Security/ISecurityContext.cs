using Amqp;

namespace Xim.Simulators.ServiceBus
{
    internal interface ISecurityContext
    {
        void Authorize(Connection connection);

        bool IsAuthorized(Connection connection);
    }
}
