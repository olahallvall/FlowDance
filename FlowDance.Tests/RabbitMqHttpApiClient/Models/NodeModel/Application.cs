using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.NodeModel
{
    public class Application
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}