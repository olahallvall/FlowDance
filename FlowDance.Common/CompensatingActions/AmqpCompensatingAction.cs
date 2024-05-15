using System.Collections.Generic;

namespace FlowDance.Common.CompensatingActions
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="virtualHost"></param>
        /// <param name="queueName"></param>
        public AmqpCompensatingAction(string virtualHost, string queueName)
        {
            VirtualHost = virtualHost;
            QueueName = queueName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="virtualHost"></param>
        /// <param name="queueName"></param>
        /// <param name="messageData"></param>
        public AmqpCompensatingAction(string virtualHost, string queueName, string messageData)
        {
            VirtualHost = virtualHost;
            QueueName = queueName;
            MessageData = messageData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="virtualHost"></param>
        /// <param name="queueName"></param>
        /// <param name="messageData"></param>
        /// <param name="headers"></param>
        public AmqpCompensatingAction(string virtualHost, string queueName, string messageData, Dictionary<string, string> headers)
        {
            VirtualHost = virtualHost;
            QueueName = queueName;
            MessageData = messageData;
            Headers = headers;
        }
    }
}
