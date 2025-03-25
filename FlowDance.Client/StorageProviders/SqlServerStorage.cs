using FlowDance.Common.Commands;
using FlowDance.Common.Events;
using FlowDance.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FlowDance.Client.StorageProviders
{
    public class SqlServerStorage : IStorageProvider
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SqlServerStorage> _logger;

        public SqlServerStorage(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<SqlServerStorage>();
        }

        public Task<SpanCommand> StoreCommandAsync(SpanCommand spanCommand)
        {
            throw new System.NotImplementedException();
        }

        public Task<SpanEvent> StoreEventInQueueAsync(SpanEvent spanEvent)
        {
            throw new System.NotImplementedException();
        }

        public Task<SpanEvent> StoreEventInStreamAsync(SpanEvent spanEvent)
        {
            throw new System.NotImplementedException();
        }
    }
}
