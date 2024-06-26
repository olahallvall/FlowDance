﻿using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IAck
    {
        [JsonProperty("ack")]
        long Ack { get; set; }

        [JsonProperty("ack_details")]
        RateDetails AckDetails { get; set; }
    }
}