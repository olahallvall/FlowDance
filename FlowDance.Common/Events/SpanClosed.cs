namespace FlowDance.Common.Events
{
    public class SpanClosed : SpanEvent
    {
        public bool MarkedAsCommitted { get; set; }
    }
}
