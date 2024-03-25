using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FlowDance.AzureFunctions
{
    public class Function
    {
        private readonly ILogger _logger;

        public Function(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function>();
        }

        [Function("Function")]
        public void Run([RabbitMQTrigger("TransactGuard.DetermineCompensation", ConnectionStringSetting = "http:::localhost:15672")] string myQueueItem)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
