using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IDeliverGet
    {
        [JsonProperty("deliver_get")]
        long DeliverGet { get; set; }

        [JsonProperty("deliver_get_details")]
        RateDetails DeliverGetDetails { get; set; }

    }
}