﻿using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IDeliverNoAck
    {
        [JsonProperty("deliver_no_ack")]
        long DeliverNoAck { get; set; }

        [JsonProperty("deliver_no_ack_details")]
        RateDetails DeliverNoAckDetails { get; set; }
    }
}