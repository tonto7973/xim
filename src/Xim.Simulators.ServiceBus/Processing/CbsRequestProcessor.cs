using System;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Microsoft.Extensions.Logging;

namespace Xim.Simulators.ServiceBus.Processing
{
    internal class CbsRequestProcessor : IRequestProcessor
    {
        private readonly ISecurityContext _messageContext;
        private readonly ILogger _logger;
        private readonly ITokenValidator _tokenValidator;

        public int Credit => 100;

        public CbsRequestProcessor(ISecurityContext messageContext, ILoggerProvider loggerProvider, ITokenValidator tokenValidator)
        {
            _messageContext = messageContext;
            _logger = loggerProvider.CreateLogger(nameof(CbsRequestProcessor));
            _tokenValidator = tokenValidator;
        }

        public void Process(RequestContext requestContext)
        {
            if (ValidateCbsRequest(requestContext))
            {
                _messageContext.Authorize(requestContext.Link.Session.Connection);
                using (var message = GetResponseMessage(200, requestContext))
                {
                    requestContext.Complete(message);
                }
            }
            else
            {
                using (var message = GetResponseMessage(401, requestContext))
                {
                    requestContext.Complete(message);
                }
                requestContext.ResponseLink.Close();
                requestContext.ResponseLink.AddClosedCallback((sender, _) => ((Link)sender).Session.Connection.CloseAsync());
            }
        }

        private bool ValidateCbsRequest(RequestContext requestContext)
        {
            var token = (string)requestContext.Message.Body;
            try
            {
                _tokenValidator.Validate(token);
                _logger.LogDebug($"Valid $cbs request; {token}.");
                return true;
            }
            catch (ArgumentException e)
            {
                _logger.LogError(e, $"Failed to validate $cbs request; {token}.");
                return false;
            }
        }

        private static Message GetResponseMessage(int responseCode, RequestContext requestContext)
            => new Message
            {
                Properties = new Properties
                {
                    CorrelationId = requestContext.Message.Properties.MessageId
                },
                ApplicationProperties = new ApplicationProperties
                {
                    ["status-code"] = responseCode
                }
            };
    }
}
