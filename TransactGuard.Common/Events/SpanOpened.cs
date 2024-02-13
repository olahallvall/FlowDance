namespace TransactGuard.Common.Events;

public class SpanOpened : Span
{
    public bool isRootSpan { get; set; }

    public string SpanCompensationUrl { get; set; }
}
