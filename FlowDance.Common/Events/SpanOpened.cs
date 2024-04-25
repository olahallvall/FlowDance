namespace FlowDance.Common.Events
{
    public class SpanOpened : SpanEvent
    {
        public bool IsRootSpan { get; set; }

        public string CompensationUrl { get; set; }
        
        public string callingFunctionName { get; set; }
    }
}
