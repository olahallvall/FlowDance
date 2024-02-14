namespace TransactGuard.Common.Events;

public class SpanClosed : Span
{
  public bool Committed { get; set; }
}
