namespace FlowDance.Common.Events;

public class SpanOpened : Span
{
    public bool IsRootSpan { get; set; }

    public required string SpanCompensationUrl { get; set; }
}
