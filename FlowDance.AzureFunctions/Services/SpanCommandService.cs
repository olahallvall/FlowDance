using FlowDance.Common.Events;
using FlowDance.Common.Models;
using Microsoft.DurableTask.Client;
using FlowDance.Common.Commands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace FlowDance.AzureFunctions.Services
{
    public interface ISpanCommandService
    {
        public void ExecuteSpanCommand(string message, DurableTaskClient durableTaskClient);
    }

    public class SpanCommandService : ISpanCommandService
    {
        private readonly ILogger _logger;
        private readonly IStorageService _storageService;
        private readonly IDistributedCache _distributedCache;

        public SpanCommandService(ILoggerFactory loggerFactory, IStorageService storage, IDistributedCache distributedCache)
        {
            _logger = loggerFactory.CreateLogger<SpanCommandService>();
            _storageService = storage;
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
                    throw new Exception("Missing Command type.");
            }
        }

        private void DetermineCompensation(string streamName, DurableTaskClient durableTaskClient)
        {
            // Build a list of Spans from Span events.
            var spanEventList = _storageService.ReadAllSpanEventsFromStream(streamName);
            var spanList = new List<Span>();

            if (spanEventList.Any())
            {
                _logger.LogInformation("Stream has {count} events!", spanEventList.Count);

                // Construct a SpanList from SpanEventList
                var spanOpenEvents = from so in spanEventList
                                     where so.GetType() == typeof(SpanOpened)
                                     select (SpanOpened)so;

                // Pick all SpanOpened event and create a Span for each
                spanList.AddRange(spanOpenEvents.Select(spanOpenEvent => new Span()
                {
                    SpanOpened = spanOpenEvent,
                    SpanId = spanOpenEvent.SpanId,
                    TraceId = spanOpenEvent.TraceId
                }));

                // Try to find a SpanClosed for each SpanOpened and group them in the same Span-instance. 
                foreach (var span in spanList)
                {
                    var spanClosedEvent = (from sc in spanEventList
                        where sc.GetType() == typeof(SpanClosed) && sc.SpanId == span.SpanOpened.SpanId
                        select (SpanClosed)sc).ToList();

                    if (spanClosedEvent.Any())
                    {
                        span.SpanClosed = spanClosedEvent.First();
                    }

                    // Find and add all CompensationData event belonging to this Span.
                    var spanCompensationDataEvents = from cd in spanEventList
                                                     where cd.GetType() == typeof(SpanCompensationData) && cd.SpanId == span.SpanOpened.SpanId
                                                     select (SpanCompensationData)cd;

                    foreach(SpanCompensationData compensationData in spanCompensationDataEvents)
                    {
                        span.CompensationData.Add(compensationData); 
                    }
                }

                // Validate that every Span has a valid SpanOpened and SpanClosed
                foreach (var span in spanList)
                {
                    if (span.SpanOpened == null || span.SpanClosed == null)
                    {
                        _logger.LogError("A Span need a valid SpanOpened and SpanClosed instance. Span with spanId {spanId} for TraceId {traceId} are missing one or both!", span.SpanId, span.TraceId);
                        throw new Exception("A Span need a valid SpanOpened and SpanClosed instance. Span with spanId {spanId} for TraceId {traceId} are missing one or both!");
                    }
                }

                // Check if we need to start the Orchestration - if things is ok is unnecessary to start an Orchestration...
                var startOrchestration = false;

                // Search for Span where MarkedAsCommitted is false
                var markedAsCommittedSpans = (from s in spanList
                    where s.SpanClosed.MarkedAsCompleted == false
                    select s).ToList();

                // Search for Span where ExceptionDetected is true
                var exceptionDetectedSpans = (from s in spanList
                    where s.SpanClosed.ExceptionDetected == true
                    select s).ToList();

                if (markedAsCommittedSpans.Any() || exceptionDetectedSpans.Any())
                    startOrchestration = true;

                if (startOrchestration)
                {
                     var json = JsonConvert.SerializeObject(spanList, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
                     string instanceId = durableTaskClient.ScheduleNewOrchestrationInstanceAsync(nameof(Sagas.CompensatingSaga), json).Result;

                    //var httpResponse = durableTaskClient.CreateCheckStatusResponse()  .CreateCheckStatusResponseAsync(req, instanceId).;

                    _logger.LogInformation("Starting CompensatingSaga with instanceId {instanceId} for traceId {traceId}", instanceId , spanList[0].SpanOpened.TraceId);
                }
                else
                    _logger.LogInformation("No CompensatingSaga was needed for traceId {traceId}", spanList[0].SpanOpened.TraceId);
            }
        }
    }
}
    