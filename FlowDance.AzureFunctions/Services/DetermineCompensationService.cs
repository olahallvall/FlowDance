using Microsoft.Extensions.Logging;

namespace FlowDance.AzureFunctions.Services
{
    public class DetermineCompensationService : IDetermineCompensation
    {
        private readonly ILogger _logger;

        public DetermineCompensationService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DetermineCompensationService>();
        }

        public void DetermineCompensation(string streamName)
        {
            throw new NotImplementedException();
        }
    }
}
