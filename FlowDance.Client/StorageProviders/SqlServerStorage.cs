using FlowDance.Common.Commands;
using FlowDance.Common.Events;
using FlowDance.Common.Interfaces;
using Microsoft.Extensions.Logging;

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

        public void StoreCommand(DetermineCompensation command)
        {
            throw new System.NotImplementedException();
        }

        public void StoreEvent(SpanEvent spanEvent)
        {
            throw new System.NotImplementedException();
        }
    }
}
