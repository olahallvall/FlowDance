using FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.Common;
using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.ConsumerModel
{
    public class Consumer
    {
        [System.Text.Json.Serialization.JsonIgnore]
        [JsonProperty("arguments")]
        public Arguments Arguments { get; set; }

        [JsonProperty("prefetch_count")]
        public long PrefetchCount { get; set; }

        [JsonProperty("ack_required")]
        public bool AckRequired { get; set; }

        [JsonProperty("exclusive")]
        public bool Exclusive { get; set; }

        [JsonProperty("consumer_tag")]
        public string ConsumerTag { get; set; }

        [JsonProperty("queue")]
        public Queue Queue { get; set; }

        [JsonProperty("channel_details")]
        public ChannelDetails ChannelDetails { get; set; }
    }
}
