using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.ClusterModel
{
    public class Cluster
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}