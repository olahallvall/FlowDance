using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IReturnUnroutable
    {
        [JsonProperty("return_unroutable")]
        long ReturnUnroutable { get; set; }

        [JsonProperty("return_unroutable_details")]
        RateDetails ReturnUnroutableDetails { get; set; }
    }
}