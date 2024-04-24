using FlowDance.Common.Events;

namespace FlowDance.Common.Models
{
    public class Span
    {
        public SpanOpened SpanOpened { get; set; }
        public SpanClosed SpanClosed { get; set; }
    }
}
