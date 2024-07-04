using Microsoft.DurableTask.Client;
using FlowDance.Common.Commands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using FlowDance.Common.Exceptions;

namespace FlowDance.Server.Services
{
    public interface ISpanCommandService
    {
        public void ExecuteSpanCommand(string message, DurableTaskClient durableTaskClient);
    }

    public class SpanCommandService : ISpanCommandService
    {
        private readonly ILogger _logger;
        private readonly IStorageStreamService _storageService;
        private readonly IDistributedCache _distributedCache;
        private readonly ISpanEventUtilService _spanEventUtilService;
         
        public SpanCommandService(ILoggerFactory loggerFactory, IStorageStreamService storage, IDistributedCache distributedCache, ISpanEventUtilService spanEventUtilService)
        {
            _logger = loggerFactory.CreateLogger<SpanCommandService>();
            _storageService = storage;
            _distributedCache = distributedCache;
            _spanEventUtilService = spanEventUtilService;
        }

        public void ExecuteSpanCommand(string message, DurableTaskClient durableTaskClient)
        {
            var spanCommand = JsonConvert.DeserializeObject(message, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

            switch(spanCommand)
            {
                case DetermineCompensationCommand determineCompensation:
                    {
                        DetermineCompensation(determineCompensation.TraceId.ToString(), durableTaskClient);
                    }
                    break;
                default:
                    throw new ExecuteSpanCommandException("Missing Command type.");
            }
        }

        /// <summary>
        /// Should we compensate or not..
        /// </summary>
        /// <param name="streamName"></param>
        /// <param name="durableTaskClient"></param>
        private void DetermineCompensation(string streamName, DurableTaskClient durableTaskClient)
        {
            var spanEventList = _storageService.ReadAllSpanEventsFromStream(streamName);
            if (spanEventList.Any())
            {
                _logger.LogInformation("Stream has {count} events!", spanEventList.Count);
                var compensationSpanList = _spanEventUtilService.CreateSpanList(spanEventList);

                // Check if we need to start the Orchestration - if things is ok is unnecessary to start an Orchestration...
                var containsSpanClosedBattered = _spanEventUtilService.SpanListContainsSpanClosedBattered(compensationSpanList);
                if (containsSpanClosedBattered)
                {
                     var json = JsonConvert.SerializeObject(compensationSpanList, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
                     string instanceId = durableTaskClient.ScheduleNewOrchestrationInstanceAsync(nameof(Sagas.CompensatingSaga), json).Result;

                    //var httpResponse = durableTaskClient.CreateCheckStatusResponse()  .CreateCheckStatusResponseAsync(req, instanceId).;

                    _logger.LogInformation("Starting CompensatingSaga with instanceId {instanceId} for traceId {traceId}", instanceId , compensationSpanList[0].SpanOpened.TraceId);
                }
                else
                    _logger.LogInformation("No CompensatingSaga was needed for traceId {traceId}", compensationSpanList[0].SpanOpened.TraceId);
            }
        }
    }
}
    