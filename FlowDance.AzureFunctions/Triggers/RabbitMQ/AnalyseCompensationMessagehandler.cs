using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FlowDance.Common.Commands;
using FlowDance.AzureFunctions.Services;
using Microsoft.DurableTask.Client;

namespace FlowDance.AzureFunctions.Triggers.RabbitMQ
{
    public class AnalyseCompensationMessagehandler
    {
        private readonly ILogger _logger;
        private readonly IAnalyseSpanEventService _analyseSpanEventService;

        public AnalyseCompensationMessagehandler(ILoggerFactory loggerFactory, IAnalyseSpanEventService analyseSpanEventService)
        {
            _logger = loggerFactory.CreateLogger<AnalyseCompensationMessagehandler>();
            _analyseSpanEventService = analyseSpanEventService;
        }

        [Function("AnalyseCompensationMessagehandler")]
        public void Run(
                [RabbitMQTrigger("FlowDance.AnalyseSpanEvent", 
                ConnectionStringSetting = "RabbitMq_Connection")] string queueItem,
                [DurableClient] DurableTaskClient durableTaskClient,
                FunctionContext context)
        {
            var analyseSpanEventCommand = JsonConvert.DeserializeObject<AnalyseSpanEvent>(queueItem);

            _analyseSpanEventService.AnalyseSpanEvent(analyseSpanEventCommand.TraceId.ToString(), durableTaskClient);

            _logger.LogInformation($"C# Queue trigger function processed: {queueItem}");
        }
    }
}
