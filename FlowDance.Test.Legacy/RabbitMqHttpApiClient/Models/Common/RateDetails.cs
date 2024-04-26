using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.Common
{
    public class RateDetails
    {
        [JsonProperty("rate")]
        public double Rate { get; set; }
    }
}