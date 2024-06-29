using FlowDance.Common.Events;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Stream.Client;

namespace FlowDance.AzureFunctions.Services
{
    public interface IAnalyseSpanEventService
    {
        public void AnalyseSpanEvent(string streamName, DurableTaskClient durableTaskClient);
    }

    public class AnalyseSpanEventService : IAnalyseSpanEventService
    {
        private readonly ILogger _logger;
        private readonly IStorage _storage;

        public AnalyseSpanEventService(ILoggerFactory loggerFactory, IStorage storage)
        {
            _logger = loggerFactory.CreateLogger<AnalyseSpanEventService>();
            _storage = storage;
        }

        public void AnalyseSpanEvent(string streamName, DurableTaskClient durableTaskClient)
        {
            throw new NotImplementedException();
        }
    }
}
    