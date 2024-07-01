using FlowDance.Common.Commands;
using FlowDance.Common.Events;

namespace FlowDance.Common.Interfaces
{
    public interface IStorageProvider
    {
        public SpanEvent StoreEventInStream(SpanEvent spanEvent);
        public SpanEvent StoreEventInQueue(SpanEvent spanEvent);
        public SpanCommand StoreCommand(SpanCommand spanCommand);
    }
}
