using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.ExchangeModel.PublishMessageModel
{
    public class PublishMessageResponse
    {
        [JsonProperty("routed")]
        public bool Routed { get; set; }
    }
}