using FlowDance.Common.Events;
using FlowDance.Common.Models;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        public SpanEventService(ILoggerFactory loggerFactory, IStorageService storage)
        {
            _logger = loggerFactory.CreateLogger<SpanEventService>();
            _storageService = storage;
        }

        public void ExecuteSpanEvent(string message, DurableTaskClient durableTaskClient)
        {
            var spanEvent = JsonConvert.DeserializeObject(message, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

            switch (spanEvent)
            {
                case SpanClosedBattered spanClosedBattered:
                    {
                        SpanClosedBattered(spanClosedBattered.TraceId.ToString(), durableTaskClient);
                    }
                    break;
                default:
                    throw new Exception("Missing Event type.");
            }
        }

        private void SpanClosedBattered(string streamName, DurableTaskClient durableTaskClient)
        {
            // Build a list of Spans from Span events.
            var spanEventList = _storageService.ReadAllSpanEventsFromStream(streamName);
            var spanList = new List<Span>();

            
        }
    }
}
    