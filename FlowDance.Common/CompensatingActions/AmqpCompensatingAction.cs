using System.Collections.Generic;

namespace FlowDance.Common.CompensatingActions
{
    /// <summary>
    /// Compensating action for RabbitMQ.  
    /// </summary>
    public class AmqpCompensatingAction : CompensatingAction
    {
        public string QueueName;
        public string MessageData;
        public Dictionary<string, string> Headers;

        public AmqpCompensatingAction()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        public AmqpCompensatingAction(string queueName)
        {    
            QueueName = queueName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="messageData"></param>
        public AmqpCompensatingAction(string queueName, string messageData)
        {
            QueueName = queueName;
            MessageData = messageData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="messageData"></param>
        /// <param name="headers"></param>
        public AmqpCompensatingAction(string queueName, string messageData, Dictionary<string, string> headers)
        {            
            QueueName = queueName;
            MessageData = messageData;
            Headers = headers;
        }
    }
}
