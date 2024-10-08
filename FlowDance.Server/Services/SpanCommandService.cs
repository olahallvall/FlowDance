﻿using Microsoft.DurableTask.Client;
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
                _logger.LogDebug("Stream has {count} events!", spanEventList.Count);
                var compensationSpanList = _spanEventUtilService.CreateSpanList(spanEventList);

                // Validate that every Span has a valid SpanOpened and SpanClosed
                //foreach (var span in compensationSpanList)
                //{
                //    if (span.SpanOpened == null || span.SpanClosed == null)
                //    {
                //        _logger.LogError("A Span need a valid SpanOpened and SpanClosed instance. Span with spanId {spanId} for TraceId {traceId} are missing one or both!", span.SpanId, span.TraceId);
                //        throw new SpanListValidationException("A Span need a valid SpanOpened and SpanClosed instance. Span with spanId {spanId} for TraceId {traceId} are missing one or both!");
                //    }
                //}

                // Check if we need to start the Orchestration - if things is ok is unnecessary to start an Orchestration...
                var containsSpanClosedBattered = _spanEventUtilService.SpanListContainsSpanClosedBattered(compensationSpanList);
                if (containsSpanClosedBattered)
                {
                     var json = JsonConvert.SerializeObject(compensationSpanList, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
                     string instanceId = durableTaskClient.ScheduleNewOrchestrationInstanceAsync(nameof(Sagas.CompensatingSaga), json).Result;

                    //var httpResponse = durableTaskClient.CreateCheckStatusResponse()  .CreateCheckStatusResponseAsync(req, instanceId).;

                    _logger.LogDebug("Starting CompensatingSaga with instanceId {instanceId} for traceId {traceId}", instanceId , compensationSpanList[0].SpanOpened.TraceId);
                }
                else
                    _logger.LogDebug("No CompensatingSaga was needed for traceId {traceId}", compensationSpanList[0].SpanOpened.TraceId);
            }
        }
    }
}
    