using System.Collections.Generic;

namespace FlowDance.Common.CompensatingActions
{
    /// <summary>
    /// Compensating action for RabbitMQ.  
    /// </summary>
    public class AmqpCompensatingAction : CompensatingAction
    {
        public string QueueName;
        public string CompensationData;
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
        /// <param name="compensationData"></param>
        public AmqpCompensatingAction(string queueName, string compensationData)
        {
            QueueName = queueName;
            CompensationData = compensationData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="compensationData"></param>
        /// <param name="headers"></param>
        public AmqpCompensatingAction(string queueName, string compensationData, Dictionary<string, string> headers)
        {            
            QueueName = queueName;
            CompensationData = compensationData;
            Headers = headers;
        }
    }
}
