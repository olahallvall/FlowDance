using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowDance.Tests.RabbitMqHttpApiClient.API;

namespace FlowDance.Tests
{
    [TestClass]

    public class RabbitMqManagementStuff
    {
        [TestMethod]
        public void DeleteAllStreamsNamnedWithGuid()
        {

            var rabbitMqApi = new RabbitMqApi("http://localhost:15672", "guest", "guest");

            //var queues = rabbitMqApi.GetQueues().Result;
            var queues = rabbitMqApi.GetQueuesByVhost("/").Result;
            foreach (var messageQueue in queues)
            {
                if (Guid.TryParseExact(messageQueue.Name, "D", out var newGuid))
                {
                    var result = rabbitMqApi.DeleteQueue("/", messageQueue.Name).Result;
                }
            }
        }

        [TestMethod]
        public void CountNumberOfMessages()
        {
            var rabbitMqApi = new RabbitMqApi("http://localhost:15672", "guest", "guest");

            var queue = rabbitMqApi.GetQueueByVhostAndName("/", "63be8788-7283-455b-9f76-050eba9168ba").Result;
            if(queue != null)
            {
                var numberOfMessages = queue.MessagesReady;
            }

                
        }
    }
}
