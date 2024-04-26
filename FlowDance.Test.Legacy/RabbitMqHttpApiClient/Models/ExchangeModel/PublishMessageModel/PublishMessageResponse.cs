using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.ExchangeModel.PublishMessageModel
{
    public class PublishMessageResponse
    {
        [JsonProperty("routed")]
        public bool Routed { get; set; }
    }
}