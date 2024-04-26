using FlowDance.Common.Events;

namespace FlowDance.AzureFunctions.Services
{
    public interface IStorage
    {
       public List<SpanEvent> ReadAllSpanEventsFromStream(string streamName);

    }
}
