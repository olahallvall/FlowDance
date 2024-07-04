using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public class VirtualHost
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
