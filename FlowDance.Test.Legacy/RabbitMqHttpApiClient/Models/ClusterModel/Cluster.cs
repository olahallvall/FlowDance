using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.ClusterModel
{
    public class Cluster
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}