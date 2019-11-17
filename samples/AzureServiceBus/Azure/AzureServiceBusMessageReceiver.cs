using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace AzureServiceBusSample.Azure
{
    public class AzureServiceBusMessageReceiver
    {
        private readonly AzureServiceBusSettings _settings;
        private readonly IMessageHandler _messageHandler;

        public AzureServiceBusMessageReceiver(AzureServiceBusSettings settings, IMessageHandler messageHandler)
        {
            _settings = settings;
            _messageHandler = messageHandler;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var receiver = new MessageReceiver(_settings.InConnectionString, _settings.InQueueName);
            try
            {
                receiver.RegisterMessageHandler(
                    async (message, handlerCancellationToken) =>
                    {
                        handlerCancellationToken.ThrowIfCancellationRequested();
                        await _messageHandler
                            .HandleMessageAsync(message)
                            .ConfigureAwait(false);
                    },
                    new MessageHandlerOptions(HandleErrors)
                    {
                        AutoComplete = true,
                        MaxConcurrentCalls = 10
                    }
                );

                await Task
                    .Run(() => cancellationToken.WaitHandle.WaitOne())
                    .ConfigureAwait(false);
            }
            finally
            {
                await receiver
                    .CloseAsync()
                    .ConfigureAwait(false);
            }
        }

        private Task HandleErrors(ExceptionReceivedEventArgs e)
        {
            if (!(e.Exception is OperationCanceledException))
            {
                // log exception
            }
            return Task.CompletedTask;
        }
    }
}
