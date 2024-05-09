using FlowDance.Common.Models;

namespace FlowDance.Common.Events
{
    public class SpanOpened : SpanEvent
    {
        public bool IsRootSpan { get; set; }

        public CompensatingAction CompensatingAction { get; set; }

        public string CallingFunctionName { get; set; }
    }
}
