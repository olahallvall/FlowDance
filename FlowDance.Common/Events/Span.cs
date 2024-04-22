using System;

namespace FlowDance.Common.Events
{
    public class Span
    {
        public Guid TraceId { get; set; }

        public Guid SpanId { get; set; }

        public DateTime Timestamp { get; set; }
    }

}