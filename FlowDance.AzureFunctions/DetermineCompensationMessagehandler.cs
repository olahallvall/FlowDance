using System;
using System.Globalization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FlowDance.Common;
using FlowDance.Common.Commands;
using System.Security.Principal;

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
        public void Run([RabbitMQTrigger("FlowDance.DetermineCompensation", ConnectionStringSetting = "FlowDanceRabbitMqConnection")] string queueItem)
        {
            var determineCompensationCommand = JsonConvert.DeserializeObject<DetermineCompensation>(queueItem);

            _logger.LogInformation($"C# Queue trigger function processed: {queueItem}");
        }
    }
}
