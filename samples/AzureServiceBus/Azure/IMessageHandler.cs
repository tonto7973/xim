using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace AzureServiceBusSample.Azure
{
    public interface IMessageHandler
    {
        Task HandleMessageAsync(Message message);
    }
}