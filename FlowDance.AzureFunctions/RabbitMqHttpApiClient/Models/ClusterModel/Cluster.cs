using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.ClusterModel
{
    public class Cluster
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}