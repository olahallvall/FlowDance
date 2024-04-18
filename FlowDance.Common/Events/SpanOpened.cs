namespace FlowDance.Common.Events
{
    public class SpanOpened : Span
    {
        public bool IsRootSpan { get; set; }

        public string CompensationUrl { get; set; }
    }
}