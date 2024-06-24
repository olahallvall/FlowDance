using FlowDance.Common.Commands;
using FlowDance.Common.Events;

namespace FlowDance.Common.Interfaces
{
    public interface IStorage
    {
        public void StoreEvent(SpanEvent spanEvent);
        public void StoreCommand(DetermineCompensation command);
    }
}
