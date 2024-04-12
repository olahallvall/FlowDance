using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FlowDance.AzureFunctions
{
    public class DetermineCompensationMessagehandler
    {
        private readonly ILogger _logger;

        public DetermineCompensationMessagehandler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DetermineCompensationMessagehandler>();
        }

        [Function("DetermineCompensationMessagehandler")]
        public void Run([RabbitMQTrigger("FlowDance.DetermineCompensation", ConnectionStringSetting = "FlowDanceRabbitMqConnection")] string myQueueItem)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
