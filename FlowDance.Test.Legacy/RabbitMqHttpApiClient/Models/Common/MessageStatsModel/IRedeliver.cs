using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IRedeliver
    {
        [JsonProperty("redeliver")]
        long Redeliver { get; set; }

        [JsonProperty("redeliver_details")]
        RateDetails RedeliverDetails { get; set; }
    }
}