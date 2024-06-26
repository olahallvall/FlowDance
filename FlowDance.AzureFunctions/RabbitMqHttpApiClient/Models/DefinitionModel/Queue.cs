﻿using FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.Common;
using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.DefinitionModel
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

        [JsonIgnore]
        [JsonProperty("arguments")]
        public Arguments Arguments { get; set; }
    }

}
