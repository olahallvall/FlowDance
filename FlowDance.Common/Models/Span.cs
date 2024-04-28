using FlowDance.Common.Events;
using System;

namespace FlowDance.Common.Models
{
    public class Span
    {
        public Guid TraceId { get; set; }
        public Guid SpanId { get; set; }

        public SpanOpened SpanOpened { get; set; }
        public SpanClosed SpanClosed { get; set; }
    }
}
