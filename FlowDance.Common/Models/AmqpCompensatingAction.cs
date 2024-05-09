using System;

namespace FlowDance.Common.Models
{
    public class AmqpCompensatingAction : CompensatingAction
    {
        private readonly string _virtualHost;
        private readonly string _queueName;

        private AmqpCompensatingAction()
        {
        }
        
        public AmqpCompensatingAction(string virtualHost, string queueName)
        {
            _virtualHost = virtualHost;
            _queueName = queueName;
        }

        public string Message { get; set; }
    }
}
