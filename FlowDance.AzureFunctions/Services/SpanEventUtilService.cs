using FlowDance.Common.Events;
using FlowDance.Common.Exceptions;
using FlowDance.Common.Models;
using Microsoft.Extensions.Logging;

namespace FlowDance.AzureFunctions.Services
{
    public interface ISpanEventUtilService
    {
        public List<Span> CreateSpanList(List<SpanEvent> spanEventList);
        public bool SpanListContainsSpanClosedBattered(List<Span> spanList);
    }

    public class SpanEventUtilService : ISpanEventUtilService
    {
        private readonly ILogger _logger;

        public SpanEventUtilService(ILoggerFactory loggerFactory) 
        {
            _logger = loggerFactory.CreateLogger<SpanEventUtilService>();
        }

        public List<Span> CreateSpanList(List<SpanEvent> spanEventList)
        {
            if (!spanEventList.Any())
                return new List<Span>();

            var spanList = new List<Span>();

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

                foreach (SpanCompensationData compensationData in spanCompensationDataEvents)
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
                    throw new SpanListValidationException("A Span need a valid SpanOpened and SpanClosed instance. Span with spanId {spanId} for TraceId {traceId} are missing one or both!");
                }
            }

            // Get the first Span and check that it's a RootSpan. 
            var rootSpan = spanList[0];
            if (rootSpan.SpanOpened.IsRootSpan == false)
                throw new SpanListValidationException("The first Span has to be a so called RootSpan. In the stream the first span was not a RootSpan!");
       
            return spanList;
        }

        public bool SpanListContainsSpanClosedBattered(List<Span> spanList)
        {
            // Search for Span where MarkedAsCommitted is false
            var markedAsCommittedSpans = (from s in spanList
                                          where s.SpanClosed.MarkedAsCompleted == false
                                          select s).ToList();

            // Search for Span where ExceptionDetected is true
            var exceptionDetectedSpans = (from s in spanList
                                          where s.SpanClosed.ExceptionDetected == true
                                          select s).ToList();

            if (markedAsCommittedSpans.Any() || exceptionDetectedSpans.Any())
                return true;
            else
                return false;
        }
    }
}
