using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.ExchangeModel.PublishMessageModel
{
    public class PublishMessageResponse
    {
        [JsonProperty("routed")]
        public bool Routed { get; set; }
    }
}