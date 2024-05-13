using System;

namespace FlowDance.Common.Events
{
    /// <summary>
    /// Base class for a Span event associated with creation or closing of a CompensationSpan.  
    /// </summary>
    public class SpanEvent
    {
        public Guid TraceId { get; set; }

        public Guid SpanId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}