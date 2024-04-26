using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.ExchangeModel.PublishMessageModel
{
    public class Properties
    {
        [JsonProperty("delivery_mode")]
        public int DeliveryMode { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }
    }
}