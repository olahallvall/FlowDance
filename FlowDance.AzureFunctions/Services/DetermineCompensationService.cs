using FlowDance.Common.Events;
using FlowDance.Common.Models;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace FlowDance.AzureFunctions.Services
{
    public class DetermineCompensationService : IDetermineCompensation
    {
        private readonly ILogger _logger;
        private readonly IStorage _storage;

        public DetermineCompensationService(ILoggerFactory loggerFactory, IStorage storage)
        {
            _logger = loggerFactory.CreateLogger<DetermineCompensationService>();
            _storage = storage;
        }

        public void DetermineCompensation(string streamName, DurableTaskClient orchestrationClient)
        {
            // Build a list of Spans from Span events.
            var spanEventList = _storage.ReadAllSpanEventsFromStream(streamName);
            var spanList = new List<Span>();

            if (spanEventList.Any())
            {
                _logger.LogInformation("Stream has {count} events!", spanEventList.Count);

                // Construct a SpanList from SpanEventList
                var spanOpenEvents = from so in spanEventList
                                       where so.GetType() == typeof(SpanOpened)
                                       select (SpanOpened)so;

                // Pick all SpanOpened event and create a Span for each
                spanList.AddRange(spanOpenEvents.Select(spanOpenEvent => new Span() {SpanOpened = spanOpenEvent}));

                foreach (var span in spanList)
                {
                    var spanClosedEvent = (from sc in spanEventList
                        where sc.GetType() == typeof(SpanClosed) && sc.SpanId == span.SpanOpened.SpanId
                        select (SpanClosed)sc).ToList();

                    if (spanClosedEvent.Any())
                    {
                        span.SpanClosed = spanClosedEvent.First();
                    }
                }

                // Start the Saga
                string instanceId = orchestrationClient.ScheduleNewOrchestrationInstanceAsync(nameof(Sagas.CompensatingSaga), spanList).Result;
            }
        }
    }
}
