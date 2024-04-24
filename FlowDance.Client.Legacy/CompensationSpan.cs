using System;
using System.Configuration;
using FlowDance.Client.Legacy.RabbitMq;
using FlowDance.Common.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FlowDance.Client.Legacy
{
    /// <summary>
    /// The CompensationSpan class provides a simple way to mark a block of code as participating in a flow dance/transaction that can be compensated.
    /// FlowDance.Client use a implicit programming model using the CompensationSpan class, in which compensating code blocks can be enlisted together using the same TraceId.
    ///
    /// Voting inside a nested scope
    /// Although a nested scope can join the ambient transaction (using the same TraceId) of the root scope, calling Complete in the nested scope has no affect on the root scope. 
    /// </summary>
    public class CompensationSpan : ICompensationSpan
    {
        private bool _disposedValue;

        private readonly Common.Events.SpanOpened _spanOpened;
        private Common.Events.SpanClosed _spanClosed;
        private bool _completed;
        private readonly Storage _rabbitMqUtil;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        private CompensationSpan()
        {
        }

        public CompensationSpan(string compensationUrl, Guid traceId, ILoggerFactory loggerFactory)
        {
            var connectionFactory = new ConnectionFactory();
         
            var hostName = ConfigurationManager.AppSettings["RabbitMqConnection.HostName"];
            var username = ConfigurationManager.AppSettings["RabbitMqConnection.Username"];
            var password = ConfigurationManager.AppSettings["RabbitMqConnection.Password"];
            var virtualHost = ConfigurationManager.AppSettings["RabbitMqConnection.VirtualHost"];

            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _rabbitMqUtil = new Storage(loggerFactory);

            // Create the event - SpanEventOpened
            _spanOpened = new Common.Events.SpanOpened() { TraceId = traceId, SpanId = Guid.NewGuid(), CompensationUrl = compensationUrl, Timestamp = DateTime.Now };

            // Store the SpanEventOpened event
            _rabbitMqUtil.StoreEvent(_spanOpened, _connection, _channel);
        }

        /// <summary>
        /// When you are satisfied that all operations within the scope are completed successfully, you should call this method only once to 
        /// inform that transaction manager that the state across all resources is consistent, and the transaction can be committed. 
        /// It is very good practice to put the call as the last statement in the using block. If not, the Span will be called for compensation.
        /// </summary>
        public void Complete()
        {
            _completed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Create the event - SpanClosed
                    _spanClosed = new Common.Events.SpanClosed() { TraceId = _spanOpened.TraceId, SpanId = _spanOpened.SpanId, MarkedAsCommitted = _completed, Timestamp = DateTime.Now };

                    // Store the SpanClosed event and calculates IsRootSpan
                    _rabbitMqUtil!.StoreEvent(_spanClosed, _connection, _connection.CreateModel());
                    
                    // Check if this is a RootSpan, if so determine compensation.
                    if (_spanOpened.IsRootSpan)
                    {
                        var determineCompensation = new Common.Commands.DetermineCompensation { TraceId = _spanOpened.TraceId };
                        _rabbitMqUtil.StoreCommand(determineCompensation, _channel);
                    }
                }

                if (_connection!.IsOpen)
                    _connection.Close();

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
