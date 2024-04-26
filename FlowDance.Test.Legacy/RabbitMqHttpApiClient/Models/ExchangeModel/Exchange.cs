﻿using FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.Common;
using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.ExchangeModel
{
    public class Exchange
    {
        [JsonProperty("message_stats")]
        public MessageStats MessageStats { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("vhost")]
        public string Vhost { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("durable")]
        public bool Durable { get; set; }

        [JsonProperty("auto_delete")]
        public bool AutoDelete { get; set; }

        [JsonProperty("internal")]
        public bool Internal { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [JsonProperty("arguments")]
        public Arguments Arguments { get; set; }
    }

}
