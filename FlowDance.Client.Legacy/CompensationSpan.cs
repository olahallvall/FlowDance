using System;
using System.Configuration;
using System.Runtime.InteropServices;
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

        public CompensationSpan(string compensationUrl, Guid traceId, ILoggerFactory loggerFactory, [System.Runtime.CompilerServices.CallerMemberName] string callingFunctionName = "")
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = ConfigurationManager.AppSettings["RabbitMqConnection.HostName"],
                UserName = ConfigurationManager.AppSettings["RabbitMqConnection.Username"],
                Password = ConfigurationManager.AppSettings["RabbitMqConnection.Password"],
                VirtualHost = ConfigurationManager.AppSettings["RabbitMqConnection.VirtualHost"]
            };

            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _rabbitMqUtil = new Storage(loggerFactory);

            // Create the event - SpanEventOpened
            _spanOpened = new Common.Events.SpanOpened()
            {
                TraceId = traceId,
                SpanId = Guid.NewGuid(),
                CompensationUrl = compensationUrl,
                CallingFunctionName = callingFunctionName,
                Timestamp = DateTime.Now
            };

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
                    _spanClosed = new Common.Events.SpanClosed()
                    {
                        TraceId = _spanOpened.TraceId,
                        SpanId = _spanOpened.SpanId,
                        MarkedAsCommitted = _completed,
                        Timestamp = DateTime.Now,
                        ExceptionDetected = Marshal.GetExceptionCode() != 0 
                    };

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
