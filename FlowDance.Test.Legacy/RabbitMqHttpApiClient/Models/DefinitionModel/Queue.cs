using FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.Common;
using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public class Queue
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("vhost")]
        public string Vhost { get; set; }

        [JsonProperty("durable")]
        public bool Durable { get; set; }

        [JsonProperty("auto_delete")]
        public bool AutoDelete { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [JsonProperty("arguments")]
        public Arguments Arguments { get; set; }
    }

}
