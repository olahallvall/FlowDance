﻿using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.Sagas
{
    public class CompensatingSaga
    {
        [Function(nameof(CompensatingSaga))]
        public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger(nameof(CompensatingSaga));
            var json = context.GetInput<string>();

            var spanList = JsonConvert.DeserializeObject<List<Span>>(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            if (spanList == null || spanList.Count == 0)
            {
                logger.LogWarning("There no Spans in the SpanList! The Orchestrator has noting to work with and will exit.");
                return;
            }

            logger.LogInformation("Start CompensatingSaga for traceId {traceId}", spanList.First().TraceId);

            // Reverse the order of Spans 
            spanList.Reverse();
            foreach (var span in spanList)
            {

                switch (span.SpanOpened.CompensatingAction)
                {
                    case HttpCompensatingAction:
                        {

                        };
                        break;

                    case AmqpCompensatingAction:
                        {

                        };
                        break;

                    default:
                        // code block
                        break;
                }
            }
        }
    }
}
