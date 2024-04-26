using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.ConsumerModel
{
    public class Queue
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("vhost")]
        public string Vhost { get; set; }
    }

}
