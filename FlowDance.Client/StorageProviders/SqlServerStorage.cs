using FlowDance.Common.Commands;
using FlowDance.Common.Events;
using FlowDance.Common.Interfaces;

namespace FlowDance.Client.StorageProviders
{
    public class SqlServerStorage : IStorage
    {
        public void CreateStream(string streamName)
        {
            throw new System.NotImplementedException();
        }

        public void StoreCommand(DetermineCompensation command)
        {
            throw new System.NotImplementedException();
        }

        public void StoreEvent(SpanEvent spanEvent)
        {
            throw new System.NotImplementedException();
        }

        public bool StreamExistOrQueue(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}
