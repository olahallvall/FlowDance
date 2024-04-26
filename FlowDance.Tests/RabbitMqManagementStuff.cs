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
    }
}
