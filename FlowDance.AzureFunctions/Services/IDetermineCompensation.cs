using Microsoft.DurableTask.Client;

namespace FlowDance.AzureFunctions.Services
{
    public interface IDetermineCompensation
    {
        public void DetermineCompensation(string streamName, DurableTaskClient durableTaskClient);
    }
}
