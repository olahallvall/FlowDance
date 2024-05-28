using FlowDance.Common.Events;
using System;
using System.Collections.Generic;

namespace FlowDance.Common.Models
{
    /// <summary>
    /// Represent a Span. Includes both events; SpanOpened and SpanClosed.  
    /// </summary>
    public class Span
    {
        public Guid TraceId { get; set; }
        public Guid SpanId { get; set; }
        public SpanOpened SpanOpened { get; set; }
        public SpanClosed SpanClosed { get; set; }
        public List<SpanCompensationData> CompensationData { get; set; } = new List<SpanCompensationData>();
    }
}
