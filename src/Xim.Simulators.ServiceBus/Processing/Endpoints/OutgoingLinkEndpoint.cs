using System;
using System.Threading;
using System.Threading.Tasks;
using Amqp.Framing;
using Amqp.Listener;
using Xim.Simulators.ServiceBus.Delivering;

namespace Xim.Simulators.ServiceBus.Processing.Endpoints
{
    internal sealed class OutgoingLinkEndpoint : LinkEndpoint
    {
        private readonly IDeliveryQueue _deliveryQueue;
        private CancellationTokenSource _flowTask;

        public OutgoingLinkEndpoint(IDeliveryQueue deliveryQueue)
        {
            _deliveryQueue = deliveryQueue;
        }

        public override void OnLinkClosed(ListenerLink link, Error error)
            => CancelFlowTask();

        public override void OnFlow(FlowContext flowContext)
        {
            CancelFlowTask();

            _flowTask = new CancellationTokenSource();
            var cancellationToken = _flowTask.Token;

            Task.Run(() => SendMessages(flowContext, cancellationToken));
        }

        private void SendMessages(FlowContext flowContext, CancellationToken cancellationToken)
        {
            var messages = flowContext.Messages;
            while (messages-- > 0)
            {
                try
                {
                    var message = _deliveryQueue.Dequeue(cancellationToken);
                    flowContext.Link.SendMessage(message);
                }
                catch (OperationCanceledException e)
                    when (e.CancellationToken == cancellationToken)
                {
                    System.Diagnostics.Debug.WriteLine("Delivery queue cancelled.");
                    return;
                }
            }
        }

        public override void OnDisposition(DispositionContext dispositionContext)
        {
            _deliveryQueue.Process(dispositionContext);
            dispositionContext.Complete();
        }

        public override void OnMessage(MessageContext messageContext)
            => throw new NotSupportedException();

        private void CancelFlowTask()
        {
            _flowTask?.Cancel();
            _flowTask?.Dispose();
            _flowTask = null;
        }
    }
}
