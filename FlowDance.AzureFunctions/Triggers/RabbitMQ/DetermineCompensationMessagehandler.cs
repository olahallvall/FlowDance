using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FlowDance.Common.Commands;
using FlowDance.AzureFunctions.Services;
using Microsoft.DurableTask.Client;

namespace FlowDance.AzureFunctions.Triggers.RabbitMQ
{
    public class DetermineCompensationMessagehandler
    {
        private readonly ILogger _logger;
        private readonly IDetermineCompensationService _determineCompensationService;

        public DetermineCompensationMessagehandler(ILoggerFactory loggerFactory, IDetermineCompensationService determineCompensationService)
        {
            _logger = loggerFactory.CreateLogger<DetermineCompensationMessagehandler>();
            _determineCompensationService = determineCompensationService;
        }

        [Function("DetermineCompensationMessagehandler")]
        public void Run(
                [RabbitMQTrigger("FlowDance.DetermineCompensation", 
                ConnectionStringSetting = "RabbitMq_Connection")] string queueItem,
                [DurableClient] DurableTaskClient durableTaskClient,
                FunctionContext context)
        {
            var determineCompensationCommand = JsonConvert.DeserializeObject<DetermineCompensation>(queueItem);

            _determineCompensationService.DetermineCompensation(determineCompensationCommand.TraceId.ToString(), durableTaskClient);

            _logger.LogInformation($"C# Queue trigger function processed: {queueItem}");
        }
    }
}
