using Microsoft.Extensions.Caching.Distributed;

namespace FlowDance.Server.Caching.SqlServer
{
    public interface IDistributedCacheFlowDance : IDistributedCache
    {
        bool SetOnce(string key, byte[] value, DistributedCacheEntryOptions options);
    }
}
