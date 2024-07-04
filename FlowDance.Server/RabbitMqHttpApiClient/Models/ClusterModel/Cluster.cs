using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.ClusterModel
{
    public class Cluster
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}