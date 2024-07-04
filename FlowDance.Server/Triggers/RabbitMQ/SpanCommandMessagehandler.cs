using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FlowDance.Server.Services;
using Microsoft.DurableTask.Client;

namespace FlowDance.Server.Triggers.RabbitMQ
{
    public class SpanCommandMessagehandler
    {
        private readonly ILogger _logger;
        private readonly ISpanCommandService _spanCommandService;

        public SpanCommandMessagehandler(ILoggerFactory loggerFactory, ISpanCommandService spanCommandService)
        {
            _logger = loggerFactory.CreateLogger<SpanCommandMessagehandler>();
            _spanCommandService = spanCommandService;
        }

        [Function("SpanCommandMessagehandler")]
        public void Run(
                [RabbitMQTrigger("FlowDance.SpanCommands", 
                ConnectionStringSetting = "RabbitMq_Connection")] string queueItem,
                [DurableClient] DurableTaskClient durableTaskClient,
                FunctionContext context)
        {
            _spanCommandService.ExecuteSpanCommand(queueItem, durableTaskClient);

            _logger.LogInformation($"Queue trigger function processed: {queueItem}");
        }
    }
}
