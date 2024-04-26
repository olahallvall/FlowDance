using FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.ExchangeModel;
using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public interface IDefinition
    {
        [JsonProperty("rabbit_version")]
        string RabbitVersion { get; set; }

        [JsonProperty("policies")]
        object[] Policies { get; set; }

        [JsonProperty("queues")]
        Queue[] Queues { get; set; }

        [JsonProperty("exchanges")]
        Exchange[] Exchanges { get; set; }

        [JsonProperty("bindings")]
        Binding[] Bindings { get; set; }
    }
}