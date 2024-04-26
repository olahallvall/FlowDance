using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public class VirtualHost
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
