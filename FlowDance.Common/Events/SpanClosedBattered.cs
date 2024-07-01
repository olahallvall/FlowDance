namespace FlowDance.Common.Events
{
    /// <summary>
    /// Via a RabbitMq queue can we inform the rest of the world that a Span has been closed battered.
    /// Probably has an excpetion been thrown or the Span has not been Completed.
    /// </summary>
    public class SpanClosedBattered : SpanClosed
    {
    }
}
