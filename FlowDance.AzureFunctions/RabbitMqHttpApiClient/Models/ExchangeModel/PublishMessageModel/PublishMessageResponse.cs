using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.ExchangeModel.PublishMessageModel
{
    public class PublishMessageResponse
    {
        [JsonProperty("routed")]
        public bool Routed { get; set; }
    }
}