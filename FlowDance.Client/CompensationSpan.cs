using FlowDance.Client.StorageProviders;
using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Events;
using FlowDance.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;

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
        private IStorageProvider _storage;
        private readonly ILogger<CompensationSpan> _logger;

        /// <summary>
        /// Creates a CompensationSpan that use http/https for accessing the Compensating Action. 
        /// </summary>
        /// <param name="httpAction"></param>
        /// <param name="traceId"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="callingFunctionName"></param>
        public CompensationSpan(HttpCompensatingAction httpAction, Guid traceId, ILoggerFactory loggerFactory, [System.Runtime.CompilerServices.CallerMemberName] string callingFunctionName = "")
        {
            _logger = loggerFactory.CreateLogger<CompensationSpan>();

            CreateSpanStorage(loggerFactory);

            StoreSpanOpened(httpAction, traceId, loggerFactory, callingFunctionName);
        }

        /// <summary>
        /// Creates a CompensationSpan that use amqp (RabbitMQ) for accessing the Compensating Action.
        /// </summary>
        /// <param name="amqpAction"></param>
        /// <param name="traceId"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="callingFunctionName"></param>
        public CompensationSpan(AmqpCompensatingAction amqpAction, Guid traceId, ILoggerFactory loggerFactory, [System.Runtime.CompilerServices.CallerMemberName] string callingFunctionName = "")
        {
            _logger = loggerFactory.CreateLogger<CompensationSpan>();

            CreateSpanStorage(loggerFactory);

            StoreSpanOpened(amqpAction, traceId, loggerFactory, callingFunctionName);  
        }

        private void StoreSpanOpened(CompensatingAction compensatingAction, Guid traceId, ILoggerFactory loggerFactory, string callingFunctionName)
        {
            _storage = new RabbitMqStorage(loggerFactory);

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
            _storage.StoreEvent(_spanOpened);
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
            _storage.StoreEvent(_spanClosed);

            // Check if this is a RootSpan, if so determine compensation.
            if (_spanOpened.IsRootSpan)
            {
                var determineCompensation = new Common.Commands.DetermineCompensation { TraceId = _spanOpened.TraceId };
                _storage.StoreCommand(determineCompensation);
            }
        }

        private static void CreateSpanStorage(ILoggerFactory loggerFactory)
        {
            var storageProviderType = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("StorageProviderType").Value;
            IStorageProvider storage = null;
            switch (storageProviderType)
            {
                case "RabbitMqStorage":
                    storage = new RabbitMqStorage(loggerFactory);
                    break;
                case "SqlServerStorage":
                    storage = new SqlServerStorage(loggerFactory);
                    break;
                default:
                    throw new Exception("Invalid StorageProviderType type in appsettings.json");
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
            _storage.StoreEvent(spanCompensationData);
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
        /// Returns the TraceId.
        /// </summary>
        public Guid TraceId => _spanOpened.TraceId;

        /// <summary>
        /// When you are satisfied that all operations within the span are completed successfully, you should call this method to 
        /// inform that the state across all resources is consistent, and the transaction can be committed. 
        /// It is very good practice to put the call as the last statement in the using block. If not, the Span will be called for compensation. 
        /// </summary>
        public void Complete()
        {
            _completed = true;
        }

        /// <summary>
        /// When you are satisfied that all operations within the span are completed successfully, you should call this method to 
        /// inform that the state across all resources is consistent, and the transaction can be committed. 
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

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        /// <summary>
        /// If you use the 
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
