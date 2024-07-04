using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.Common
{
    public class RateDetails
    {
        [JsonProperty("rate")]
        public double Rate { get; set; }
    }
}