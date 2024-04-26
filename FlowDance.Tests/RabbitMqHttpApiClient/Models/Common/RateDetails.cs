using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.Common
{
    public class RateDetails
    {
        [JsonProperty("rate")]
        public double Rate { get; set; }
    }
}