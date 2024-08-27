using FlowDance.Common.Events;
using FlowDance.Common.Exceptions;
using FlowDance.Server.Caching.SqlServer;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowDance.Server.Services
{
    public interface ISpanEventService
    {
        public void ExecuteSpanEvent(string message, DurableTaskClient durableTaskClient);
    }

    public class SpanEventService : ISpanEventService
    {
        private readonly ILogger _logger;
        private readonly IStorageStreamService _storageService;
        private readonly IDistributedCache _distributedCache;
        private readonly ISpanEventUtilService _spanEventUtilService;
        private readonly IStorageQueueService _storageQueueService;
        
        public SpanEventService(ILoggerFactory loggerFactory, IStorageStreamService storage, IDistributedCache distributedCache, ISpanEventUtilService spanEventUtilService, IStorageQueueService storageQueueService)
        {
            _logger = loggerFactory.CreateLogger<SpanEventService>();
            _storageService = storage;
            _distributedCache = distributedCache;
            _spanEventUtilService = spanEventUtilService;
            _storageQueueService = storageQueueService;
        }

        /// <summary>
        /// Gets called when a event exist in the FlowDance.SpanEvents.     
        /// </summary>
        /// <param name="message"></param>
        /// <param name="durableTaskClient"></param>
        /// <exception cref="ExecuteSpanEventException"></exception>
        public void ExecuteSpanEvent(string message, DurableTaskClient durableTaskClient)
        {
            var spanEvent = JsonConvert.DeserializeObject(message, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

            switch (spanEvent)
            {
                case SpanClosedBattered spanClosedBattered:
                    {
                        SpanClosedBattered(spanClosedBattered, durableTaskClient);
                    }
                    break;
                default:
                    throw new ExecuteSpanEventException("Missing Event type.");
            }
        }

        /// <summary>
        /// Gets called when a event of the type SpanClosedBattered exist in the FlowDance.SpanEvents. 
        /// This method is idempotent and will only executes once per traceId. 
        /// </summary>
        /// <param name="spanClosedBattered"></param>
        /// <param name="durableTaskClient"></param>
        /// <exception cref="ExecuteSpanEventException"></exception>
        private void SpanClosedBattered(SpanClosedBattered spanClosedBattered, DurableTaskClient durableTaskClient)
        {
            // Idempotent check
            var streamName = spanClosedBattered.TraceId.ToString();
            var dempotencyKey = streamName + "_SpanClosedBattered";

            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddDays(7));
            var distributedCacheFlowDance = (IDistributedCacheFlowDance)_distributedCache;
            var inserted = distributedCacheFlowDance.SetOnce(dempotencyKey, Array.Empty<Byte>(), options);

            if (inserted)
            {
                _logger.LogInformation("{dempotencyKey} has been saved to cache now.", dempotencyKey);
                try
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
                            _logger.LogInformation("TraceId {traceId}, with the type CompensationSpanOption.RequiresNewNonBlockingCallChain, has one or more SpanClosedBattered and will need compensation. FlowDance will add a DetermineCompensationCommand to FlowDance.SpanCommands.", streamName);
                            var determineCompensation = new Common.Commands.DetermineCompensationCommand { TraceId = spanClosedBattered.TraceId, SpanId = spanClosedBattered.SpanId, Timestamp = spanClosedBattered.Timestamp };
                            _storageQueueService.StoreCommand(determineCompensation);
                        }
                        else
                            _logger.LogInformation("{dempotencyKey} belongs to a CompensationSpan of type CompensationSpanOption.RequiresNewBlockingCallChain and no DetermineCompensationCommand will be sent.", dempotencyKey);
                    }
                }
                catch (Exception ex)
                {
                    // SpanClosedBattered didn't run successful and we remove the Idempotent key.
                    _distributedCache.Remove(dempotencyKey);
                    _logger.LogInformation("{dempotencyKey} has been removed due to exception.", dempotencyKey);

                    throw new ExecuteSpanEventException("Someting goes bad when executing SpanClosedBattered(). Please see inner exception.", ex);
                }
            }
            _logger.LogInformation("{dempotencyKey} has been executed before! No action will be taken.", dempotencyKey);
        }
    }
}
    