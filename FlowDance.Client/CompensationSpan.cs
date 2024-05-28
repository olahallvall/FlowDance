using FlowDance.Client.RabbitMq;
using FlowDance.Common.Interfaces;
using FlowDance.Common.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Runtime.InteropServices;
using FlowDance.Common.CompensatingActions;
using System;

namespace FlowDance.Client
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

        private SpanOpened _spanOpened;
        private SpanClosed _spanClosed;
        private bool _completed;
        private Storage _rabbitMqUtil;
        private IConnection _connection;
        private IModel _channel;
        private readonly ILogger<CompensationSpan> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpAction"></param>
        /// <param name="traceId"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="callingFunctionName"></param>
        public CompensationSpan(HttpCompensatingAction httpAction, Guid traceId, ILoggerFactory loggerFactory, [System.Runtime.CompilerServices.CallerMemberName] string callingFunctionName = "")
        {
            _logger = loggerFactory.CreateLogger<CompensationSpan>();

            StoreSpanOpened(httpAction, traceId, loggerFactory, callingFunctionName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="amqpAction"></param>
        /// <param name="traceId"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="callingFunctionName"></param>
        public CompensationSpan(AmqpCompensatingAction amqpAction, Guid traceId, ILoggerFactory loggerFactory, [System.Runtime.CompilerServices.CallerMemberName] string callingFunctionName = "")
        {
            _logger = loggerFactory.CreateLogger<CompensationSpan>();

            StoreSpanOpened(amqpAction, traceId, loggerFactory, callingFunctionName);  
        }

        private void StoreSpanOpened(CompensatingAction compensatingAction, Guid traceId, ILoggerFactory loggerFactory, string callingFunctionName)
        {
            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            var connectionFactory = new ConnectionFactory();
            config.GetSection("RabbitMqConnection").Bind(connectionFactory);

            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _rabbitMqUtil = new Storage(loggerFactory);

            // Create the event - SpanEventOpened
            _spanOpened = new SpanOpened()
            {
                TraceId = traceId,
                SpanId = Guid.NewGuid(),
                CompensatingAction = compensatingAction,
                CallingFunctionName = callingFunctionName,
                Timestamp = DateTime.Now
            };

            // Store the SpanEventOpened event
            _rabbitMqUtil.StoreEvent(_spanOpened, _connection, _channel);
        }

        private void StoreSpanClosed(Guid traceId, Guid spanId, bool completed)
        {
            // Create the event - SpanClosed
            _spanClosed = new SpanClosed()
            {
                TraceId = traceId,
                SpanId = spanId,
                MarkedAsCommitted = completed,
                Timestamp = DateTime.Now,
                ExceptionDetected = Marshal.GetExceptionCode() != 0
            };

            // Store the SpanClosed event and calculates IsRootSpan
            _rabbitMqUtil.StoreEvent(_spanClosed, _connection, _connection.CreateModel());

            // Check if this is a RootSpan, if so determine compensation.
            if (_spanOpened.IsRootSpan)
            {
                var determineCompensation = new Common.Commands.DetermineCompensation { TraceId = _spanOpened.TraceId };
                _rabbitMqUtil.StoreCommand(determineCompensation, _channel);
            }
        }

        private void StoreSpanCompensationData(Guid traceId, Guid spanId, string compensationData, string compensationDataIdentifier)
        {
            // Create the event - SpanCompensationData
            var spanCompensationData = new SpanCompensationData()
            {
                TraceId = traceId,
                SpanId = spanId,
                Timestamp = DateTime.Now,
                CompensationData = compensationData,
                Identifier = compensationDataIdentifier
            };

            // Store the SpanCompensationData event 
            _rabbitMqUtil.StoreEvent(spanCompensationData, _connection, _connection.CreateModel());
        }

        /// <summary>
        /// Add a CompensationData event to the stream of events. You can have multiple CompensationData events for a span.
        /// </summary>
        public void AddCompensationData(string compensationData, string compensationDataIdentifier)
        {
            StoreSpanCompensationData(_spanOpened.TraceId, _spanOpened.SpanId, compensationData, compensationDataIdentifier);
        }

        /// <summary>
        /// Add a CompensationData event to the stream of events. You can have multiple CompensationData events for a span.
        /// </summary>
        public void AddCompensationData(string compensationData)
        {
            StoreSpanCompensationData(_spanOpened.TraceId, _spanOpened.SpanId, compensationData, String.Empty);
        }

        /// <summary>
        /// When you are satisfied that all operations within the span are completed successfully, you should call this method only once to 
        /// inform that transaction manager that the state across all resources is consistent, and the transaction can be committed. 
        /// It is very good practice to put the call as the last statement in the using block. If not, the Span will be called for compensation. 
        /// </summary>
        public void Complete()
        {
            _completed = true;
        }

        /// <summary>
        /// When you are satisfied that all operations within the span are completed successfully, you should call this method only once to 
        /// inform that transaction manager that the state across all resources is consistent, and the transaction can be committed. 
        /// It is very good practice to put the call as the last statement in the using block. If not, the Span will be called for compensation. 
        /// </summary>
        /// <param name="compensationData"></param>
        public void Complete(string compensationData, string compensationDataIdentifier)
        {
            StoreSpanCompensationData(_spanOpened.TraceId, _spanOpened.SpanId, compensationData, compensationDataIdentifier);

            _completed = true;
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    StoreSpanClosed(_spanOpened.TraceId, _spanOpened.SpanId, _completed);
                }

                if (_connection.IsOpen)
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
