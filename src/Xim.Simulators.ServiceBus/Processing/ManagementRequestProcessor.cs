using System;
using System.Linq;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using Microsoft.Extensions.Logging;

namespace Xim.Simulators.ServiceBus.Processing
{
    internal class ManagementRequestProcessor : IRequestProcessor
    {
        private readonly ILogger _logger;

        public int Credit => 100;

        public ManagementRequestProcessor(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger(nameof(ManagementRequestProcessor));
        }

        public void Process(RequestContext requestContext)
        {
            var request = requestContext.Message;
            var operation = request.ApplicationProperties?.Map["operation"] as string;
            Map messageBody = null;
            switch (operation)
            {
                case "com.microsoft:renew-lock":
                    var tokens = ((Map)request.Body)["lock-tokens"] as Guid[];
                    messageBody = new Map
                    {
                        ["expirations"] = Enumerable.Repeat(DateTime.UtcNow.AddMinutes(5), tokens.Length).ToArray()
                    };
                    _logger.LogDebug($"{operation} applied to {tokens.Length} lock token(s).");
                    break;
                default:
                    _logger.LogDebug($"Unsupported operation {operation}.");
                    break;
            }
            var response = new Message(messageBody)
            {
                ApplicationProperties = new ApplicationProperties
                {
                    ["statusCode"] = 200
                }
            };
            using (response)
            {
                requestContext.Complete(response);
            }
        }
    }
}