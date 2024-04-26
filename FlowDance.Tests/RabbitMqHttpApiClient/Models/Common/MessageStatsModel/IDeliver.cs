using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IDeliver
    {
        [JsonProperty("deliver")]
        long Deliver { get; set; }

        [JsonProperty("deliver_details")]
        RateDetails DeliverDetails { get; set; }
    }
}