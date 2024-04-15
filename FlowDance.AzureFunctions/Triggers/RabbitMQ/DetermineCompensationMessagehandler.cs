using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FlowDance.Common.Commands;
using FlowDance.AzureFunctions.Services;

namespace FlowDance.AzureFunctions.Triggers.RabbitMQ
{
    public class DetermineCompensationMessagehandler
    {
        private readonly ILogger _logger;
        private readonly IDetermineCompensation _determineCompensationService;

        public DetermineCompensationMessagehandler(ILoggerFactory loggerFactory, IDetermineCompensation determineCompensationService)
        {
            _logger = loggerFactory.CreateLogger<DetermineCompensationMessagehandler>();
            _determineCompensationService = determineCompensationService;
        }

        [Function("DetermineCompensationMessagehandler")]
        public void Run([RabbitMQTrigger("FlowDance.DetermineCompensation", ConnectionStringSetting = "FlowDanceRabbitMqConnection")] string queueItem)
        {
            var determineCompensationCommand = JsonConvert.DeserializeObject<DetermineCompensation>(queueItem);

            _determineCompensationService.DetermineCompensation(determineCompensationCommand.TraceId.ToString());

            _logger.LogInformation($"C# Queue trigger function processed: {queueItem}");
        }
    }
}
