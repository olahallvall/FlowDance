using FlowDance.Common.Events;
using FlowDance.Common.Exceptions;
using FlowDance.Common.Models;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace FlowDance.AzureFunctions.Services
{
    public interface ISpanEventService
    {
        public void ExecuteSpanEvent(string message, DurableTaskClient durableTaskClient);
    }

    public class SpanEventService : ISpanEventService
    {
        private readonly ILogger _logger;
        private readonly IStorageService _storageService;
        private readonly IDistributedCache _distributedCache;
        private readonly ISpanEventUtilService _spanEventUtilService;

        public SpanEventService(ILoggerFactory loggerFactory, IStorageService storage, IDistributedCache distributedCache, ISpanEventUtilService spanEventUtilService)
        {
            _logger = loggerFactory.CreateLogger<SpanEventService>();
            _storageService = storage;
            _distributedCache = distributedCache;
            _spanEventUtilService = spanEventUtilService;
        }

        public void ExecuteSpanEvent(string message, DurableTaskClient durableTaskClient)
        {
            var spanEvent = JsonConvert.DeserializeObject(message, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

            switch (spanEvent)
            {
                case SpanClosedBattered spanClosedBattered:
                    {
                        // Idempotent check
                        var hasBeenExecutedBefore = _distributedCache.Get(spanClosedBattered.TraceId.ToString());

                        if(hasBeenExecutedBefore == null)
                        {
                            // We assume that the SpanClosedBattered will run successful - if not the key will be removed.
                            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddDays(7));
                            _distributedCache.Set(spanClosedBattered.TraceId.ToString(), Array.Empty<Byte>(), options);

                            try
                            {
                                SpanClosedBattered(spanClosedBattered.TraceId.ToString(), durableTaskClient);
                            }
                            catch (Exception ex)
                            {
                                // SpanClosedBattered didn't run successful and we remove the Idempotent key.
                                _distributedCache.Remove(spanClosedBattered.TraceId.ToString());

                                throw new ExecuteSpanEventException("Someting goes bad when executing SpanClosedBattered(). Please see inner exception.", ex);
                            }
                        }
                    }
                    break;
                default:
                    throw new Exception("Missing Event type.");
            }
        }

        private void SpanClosedBattered(string streamName, DurableTaskClient durableTaskClient)
        {
            var spanEventList = _storageService.ReadAllSpanEventsFromStream(streamName);
            if (spanEventList.Any())
            {
                _logger.LogInformation("Stream has {count} events!", spanEventList.Count);
                var compensationSpanList = _spanEventUtilService.CreateSpanList(spanEventList);

                // Get the RootSpan. 
                var rootSpan = compensationSpanList[0];
                if (rootSpan.SpanOpened.CompensationSpanOption == Common.Enums.CompensationSpanOption.RequiresNewNonBlockingCallChain)
                {
                    // Add a message to FlowDance.SpanCommands queue. 
                    _logger.LogInformation("TraceId {traceId} has one or more SpanClosedBattered and will need compensation. FlowDance will add a DetermineCompensationCommand to FlowDance.SpanCommands.", compensationSpanList[0].SpanOpened.TraceId);
                }
            }
        }
    }
}
    