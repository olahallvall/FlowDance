﻿using FlowDance.Server.RabbitMqHttpApiClient.Models.Common;
using FlowDance.Server.RabbitMqHttpApiClient.Models.ConsumerModel;
using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.ChannelModel
{
    public class Channel
    {
        [JsonProperty("vhost")]
        public string Vhost { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("node")]
        public string Node { get; set; }

        [JsonProperty("garbage_collection")]
        public GarbageCollection GarbageCollection { get; set; }

        [JsonProperty("reductions")]
        public long Reductions { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("global_prefetch_count")]
        public long GlobalPrefetchCount { get; set; }

        [JsonProperty("prefetch_count")]
        public long PrefetchCount { get; set; }

        [JsonProperty("acks_uncommitted")]
        public long AcksUncommitted { get; set; }

        [JsonProperty("messages_uncommitted")]
        public long MessagesUncommitted { get; set; }

        [JsonProperty("messages_unconfirmed")]
        public long MessagesUnconfirmed { get; set; }

        [JsonProperty("messages_unacknowledged")]
        public long MessagesUnacknowledged { get; set; }

        [JsonProperty("consumer_count")]
        public long ConsumerCount { get; set; }

        [JsonProperty("confirm")]
        public bool Confirm { get; set; }

        [JsonProperty("transactional")]
        public bool Transactional { get; set; }

        [JsonProperty("idle_since")]
        public string IdleSince { get; set; }

        [JsonProperty("reductions_details")]
        public RateDetails ReductionsDetails { get; set; }

        [JsonProperty("message_stats")]
        public MessageStats MessageStats { get; set; }

        [JsonProperty("consumer_details")]
        public IEnumerable<Consumer> ConsumerDetails { get; set; }

        [JsonProperty("deliveries")]
        public object[] Deliveries { get; set; }

        [JsonProperty("publishes")]
        public object[] Publishes { get; set; }

        [JsonProperty("connection_details")]
        public ConnectionDetails ConnectionDetails { get; set; }
    }

}
