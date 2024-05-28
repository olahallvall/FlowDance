namespace FlowDance.Common.Events
{
    /// <summary>
    /// Holds the data associated with a SpanCompensation.  
    /// </summary>
    public class SpanCompensationData : SpanEvent
    {
        public string CompensationData { get; set; }
        public string Identifier { get; set; }
    }
}
