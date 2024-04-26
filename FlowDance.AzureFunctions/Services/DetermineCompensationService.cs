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
            if (spanEventList.Any())
            {
                _logger.LogInformation("Stream has {count} events!", spanEventList.Count);

                //// Rule #1 - Can´t add SpanEvent after the root SpanEvent has been closed.
                //var spanOpened = spanList[0];
                //var spanClosed = from s in spanList
                //    where s.SpanId == spanOpened.SpanId && s.GetType() == typeof(SpanClosed)
                //    select s;

                //if (spanClosed.Any())
                //    throw new Exception("Spans can´t be add after the root SpanEvent has been closed");
            }

            // ToDo: spanEventList need tags for serialization 
            string instanceId = orchestrationClient.ScheduleNewOrchestrationInstanceAsync(nameof(Sagas.CompensatingSaga), spanEventList).Result;
        }
    }
}
