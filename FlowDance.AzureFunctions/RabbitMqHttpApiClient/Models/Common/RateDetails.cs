using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.Common
{
    public class RateDetails
    {
        [JsonProperty("rate")]
        public double Rate { get; set; }
    }
}