using Microsoft.Extensions.Logging;

namespace FlowDance.Client;
/// <summary>
/// The CompensationScope class provides a simple way to mark a block of code as participating in a transaction that can be compensated.
/// FlowDance.Client use a implicit programming model using the CompensationScope class, in which compensating code blocks can be enlisted together using the same TraceId.
/// </summary>
public class CompensationScope : IDisposable
{
    private bool disposedValue;

    private Common.Events.SpanOpened _spanOpened = null!;
    private Common.Events.SpanClosed _spanClosed = null!;
    private bool _completed = false;
    private RabbitMQUtils.Storage _rabbitMQUtil = null!;

    private CompensationScope()
    {
    }

    public CompensationScope(string url, Guid traceId, ILoggerFactory loggerFactory) 
    {
        if (_rabbitMQUtil == null)
            _rabbitMQUtil = new RabbitMQUtils.Storage(loggerFactory);

        // Create the event - SpanOpened
        _spanOpened = new Common.Events.SpanOpened() { TraceId = traceId, SpanId = Guid.NewGuid(), SpanCompensationUrl = url };

        // Store the SpanOpended event
        _rabbitMQUtil.StoreEvent(_spanOpened);
    }

    /// <summary>
    /// When you are satisfied that all operations within the scope are completed successfully, you should call this method only once to 
    /// inform that transaction manager that the state across all resources is consistent, and the transaction can be committed. 
    /// It is very good practice to put the call as the last statement in the using block
    /// </summary>
    public void Complete() 
    {
        _completed = true;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Create the event - SpanClosed
                _spanClosed = new Common.Events.SpanClosed() { TraceId = _spanOpened.TraceId, SpanId = _spanOpened.SpanId, MarkedAsCommitted = _completed };

                // Store the SpanClosed event and calulates IsRootSpan
                _rabbitMQUtil.StoreEvent(_spanClosed);

                if (_spanOpened.IsRootSpan)
                {
                    var determineCompensation = new Common.Commands.DetermineCompensation();
                    // Check if this is a RootSpan, if so determine compensation.
                    _rabbitMQUtil.StoreCommand(determineCompensation);
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
