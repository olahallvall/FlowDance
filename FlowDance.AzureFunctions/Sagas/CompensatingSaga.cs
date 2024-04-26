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


        }
    }
}
