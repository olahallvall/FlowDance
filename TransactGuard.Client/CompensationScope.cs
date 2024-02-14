using TransactGuard.Common.RabbitMQ;

namespace TransactGuard.Client;
public class CompensationScope : IDisposable
{
    private bool disposedValue;

    private Common.Events.SpanOpened _spanOpened = null!;
    private Common.Events.SpanClosed _spanClosed = null!;
    private bool _committed = false;

    private CompensationScope()
    {
    }

    public CompensationScope(string url, Guid traceId) 
    {
        // Create the event - SpanOpened
        _spanOpened = new Common.Events.SpanOpened() { TraceId = traceId, SpanId = Guid.NewGuid(), SpanCompensationUrl = url };

        // Store the SpanOpended event
        RabbitMQUtil.StoreEvent(_spanOpened);
    }

    public void Commit() 
    {
        _committed = true;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Create the event - SpanClosed
                _spanClosed = new Common.Events.SpanClosed() { TraceId = _spanOpened.TraceId, SpanId = _spanOpened.SpanId, MarkedAsCommitted = _committed };

                // Store the SpanClosed event
                RabbitMQUtil.StoreEvent(_spanClosed);

                if(_spanOpened.IsRootSpan)
                {
                    var determineCompensation = new Common.Commands.DetermineCompensation();
                    // Check if this is a RootSpan, if so determine compensation.
                    RabbitMQUtil.StoreCommand(determineCompensation);
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~CompensationsScope()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
