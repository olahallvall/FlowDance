using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public class VirtualHost
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
