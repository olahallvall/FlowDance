using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FlowDance.AzureFunctions.Sagas
{
    public class CompensatingSaga
    {
        [Function(nameof(CompensatingSaga))]
        public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger(nameof(CompensatingSaga));
            var spanList = context.GetInput<List<Span>>();

            logger.LogInformation("Start CompensatingSaga");

            if (spanList == null)
            {
                logger.LogWarning("There no Spans in the SpanList! The Orchestrator has noting to work with an will exit.");
                return;
            }

            // Reverse the order of Spans 
            spanList.Reverse();
            foreach (var span in spanList)
            {
                // Call Compensating URL for each Span in reverse order.
                var compensationUrl = span.SpanOpened.CompensationUrl;


            }
        }
    }
}
