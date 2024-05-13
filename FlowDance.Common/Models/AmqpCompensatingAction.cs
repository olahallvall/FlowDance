using System.Collections.Generic;

namespace FlowDance.Common.Models
{
    /// <summary>
    /// Compensating action for RabbitMQ.  
    /// </summary>
    public class AmqpCompensatingAction : CompensatingAction
    {
        public string VirtualHost;
        public string QueueName;
        public string MessageData;
        public Dictionary<string, string> Headers;

        public AmqpCompensatingAction()
        {
        }

        public AmqpCompensatingAction(string virtualHost, string queueName)
        {
            VirtualHost = virtualHost;
            QueueName = queueName;
        }

        public AmqpCompensatingAction(string virtualHost, string queueName, string messageData)
        {
            VirtualHost = virtualHost;
            QueueName = queueName;
            MessageData = messageData;
        }

        public AmqpCompensatingAction(string virtualHost, string queueName, string messageData, Dictionary<string, string> headers)
        {
            VirtualHost = virtualHost;
            QueueName = queueName;
            MessageData = messageData;
            Headers = headers;
        }
    }
}
