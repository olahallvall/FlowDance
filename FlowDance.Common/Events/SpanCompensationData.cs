namespace FlowDance.Common.Events
{
    /// <summary>
    /// Holds the data associated with a SpanCompensation.  
    /// Just it as it fits you! Only your imagination sets the limit.
    /// 
    /// Maybe you shouldn't add a massive dataset here due to performance issues. 
    /// </summary>
    public class SpanCompensationData : SpanEvent
    {
        /// <summary>
        /// Store data involved in a CompensatingAction.
        /// </summary>
        public string CompensationData { get; set; }

        /// <summary>
        /// Give the data involved in a CompensatingAction a name for later look up.
        /// </summary>
        public string Identifier { get; set; }
    }
}
