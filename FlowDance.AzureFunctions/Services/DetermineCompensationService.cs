using Microsoft.Extensions.Logging;

namespace FlowDance.AzureFunctions.Services
{
    public class DetermineCompensationService : IDetermineCompensation
    {
        private readonly ILogger _logger;
        private readonly IStorage _storage;

        public DetermineCompensationService(ILoggerFactory loggerFactory, IStorage storage)
        {
            _logger = loggerFactory.CreateLogger<DetermineCompensationService>();
            _storage = storage;
        }

        public void DetermineCompensation(string streamName)
        {
            _logger.LogInformation("Steam worked !");
        }
    }
}
