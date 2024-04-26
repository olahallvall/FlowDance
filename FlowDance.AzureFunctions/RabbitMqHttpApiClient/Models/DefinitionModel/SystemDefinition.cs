﻿using FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.ExchangeModel;
using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.DefinitionModel
{
    public class SystemDefinition : IDefinition
    {
        public string RabbitVersion { get; set; }

        [JsonProperty("users")]
        public User[] Users { get; set; }

        [JsonProperty("vhosts")]
        public VirtualHost[] Vhosts { get; set; }

        [JsonProperty("permissions")]
        public Permission[] Permissions { get; set; }

        [JsonProperty("parameters")]
        public object[] Parameters { get; set; }

        public object[] Policies { get; set; }
        public Queue[] Queues { get; set; }
        public Exchange[] Exchanges { get; set; }
        public Binding[] Bindings { get; set; }
    }

}
