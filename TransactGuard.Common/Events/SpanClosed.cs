namespace TransactGuard.Common.Events;

public class SpanClosed : Span
{
    public bool MarkedAsCommitted { get; set; }
}
