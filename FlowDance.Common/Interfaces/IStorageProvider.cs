using FlowDance.Common.Commands;
using FlowDance.Common.Events;
using System.Threading.Tasks;

namespace FlowDance.Common.Interfaces
{
    public interface IStorageProvider
    {
        public Task<SpanEvent> StoreEventInStreamAsync(SpanEvent spanEvent);
        public Task<SpanEvent> StoreEventInQueueAsync(SpanEvent spanEvent);
        public Task<SpanCommand> StoreCommandAsync(SpanCommand spanCommand);
    }
}
