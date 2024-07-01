using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Enums;

namespace FlowDance.Common.Events
{
    /// <summary>
    /// Holds the data associated with the creation of a CompensationSpan.  
    /// </summary>
    public class SpanOpened : SpanEvent
    {
        public bool IsRootSpan { get; set; }

        public CompensatingAction CompensatingAction { get; set; }

        public string CallingFunctionName { get; set; }

        public CompensationSpanOption CompensationSpanOption { get; set; }
    }
}
