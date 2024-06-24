using FlowDance.Common.Commands;
using FlowDance.Common.Events;

namespace FlowDance.Common.Interfaces
{
    public interface IStorageProvider
    {
        public void StoreEvent(SpanEvent spanEvent);
        public void StoreCommand(DetermineCompensation command);
    }
}
