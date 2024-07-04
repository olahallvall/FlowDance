using FlowDance.Server.Functions;
using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FlowDance.Server.Sagas
{
    /// <summary>
    /// Generic Saga for compensate.  
    // See https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints?tabs=csharp
    /// </summary>
    public class CompensatingSaga
    {
        public CompensatingSaga()
        {
        }

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

            // Start to CallActivity...
            var tasks = new List<Task<bool>>();
            foreach (var span in spanList)
            {
                switch (span.SpanOpened.CompensatingAction)
                {
                    case HttpCompensatingAction:
                        {
                            var httpRetryPolicy = TaskOptions.FromRetryPolicy(new RetryPolicy(
                                      maxNumberOfAttempts: 3,
                                      firstRetryInterval: TimeSpan.FromSeconds(15)));

                            string spanJson = JsonConvert.SerializeObject(span, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
                            tasks.Add(context.CallActivityAsync<bool>(nameof(HttpCompensating.HttpCompensate), spanJson, httpRetryPolicy));
                        };
                        break;

                    case AmqpCompensatingAction:
                        {
                             var amqpRetryPolicy = TaskOptions.FromRetryPolicy(new RetryPolicy(
                                      maxNumberOfAttempts: 3,
                                      firstRetryInterval: TimeSpan.FromSeconds(15)));

                            string spanJson = JsonConvert.SerializeObject(span, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
                            tasks.Add(context.CallActivityAsync<bool>(nameof(RabbitMqCompensating.RabbitMqCompensate), spanJson, amqpRetryPolicy));                            
                        };
                        break;

                    default:
                        // code block
                        break;
                }
            }
            
            // Wait for all to complete.
            await Task.WhenAll(tasks);

            logger.LogInformation("Ending CompensatingSaga for traceId {traceId}", spanList.First().TraceId);
        }
    }
}
