using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FlowDance.AzureFunctions.Services;
using Microsoft.DurableTask.Client;

namespace FlowDance.AzureFunctions.Triggers.RabbitMQ
{
    public class SpanEventMessagehandler
    {
        private readonly ILogger _logger;
        private readonly ISpanEventService _spanEventService;

        public SpanEventMessagehandler(ILoggerFactory loggerFactory, ISpanEventService spanEventService)
        {
            _logger = loggerFactory.CreateLogger<SpanEventMessagehandler>();
            _spanEventService = spanEventService;
        }

        [Function("SpanEventMessagehandler")]
        public void Run(
                [RabbitMQTrigger("FlowDance.SpanEvents", 
                ConnectionStringSetting = "RabbitMq_Connection")] string queueItem,
                [DurableClient] DurableTaskClient durableTaskClient,
                FunctionContext context)
        {
            _spanEventService.ExecuteSpanEvent(queueItem, durableTaskClient);

            _logger.LogInformation($"Queue trigger function processed: {queueItem}");
        }
    }
}
