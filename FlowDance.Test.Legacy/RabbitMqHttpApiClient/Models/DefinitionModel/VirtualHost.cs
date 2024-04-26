using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public class VirtualHost
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
