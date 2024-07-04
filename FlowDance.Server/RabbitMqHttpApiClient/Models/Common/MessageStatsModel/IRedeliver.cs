using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IRedeliver
    {
        [JsonProperty("redeliver")]
        long Redeliver { get; set; }

        [JsonProperty("redeliver_details")]
        RateDetails RedeliverDetails { get; set; }
    }
}