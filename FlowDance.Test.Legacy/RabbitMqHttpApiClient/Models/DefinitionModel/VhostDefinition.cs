using FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.ExchangeModel;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public class VhostDefinition : IDefinition
    {
        public string RabbitVersion { get; set; }
        public object[] Policies { get; set; }
        public Queue[] Queues { get; set; }
        public Exchange[] Exchanges { get; set; }
        public Binding[] Bindings { get; set; }
    }
}