using System;

namespace FlowDance.Common.Commands
{
    /// <summary>
    /// Base class for a Span command.  
    /// </summary>
    public class SpanCommand
    {
        public Guid TraceId { get; set; }

        public Guid SpanId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}