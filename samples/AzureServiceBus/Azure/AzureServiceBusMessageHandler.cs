using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace AzureServiceBusSample.Azure
{
    public class AzureServiceBusMessageHandler : IMessageHandler
    {
        public Task HandleMessageAsync(Message message)
        {
            var body = Encoding.ASCII.GetString(message.Body);
            return SimulateProcessMessageBody(body);
        }

        private Task SimulateProcessMessageBody(string body)
        {
            System.Diagnostics.Debug.WriteLine(body);

            if (!body.Contains("abc"))
                throw new InvalidOperationException("Body is not valid");

            return Task.Delay(250);
        }
    }
}
