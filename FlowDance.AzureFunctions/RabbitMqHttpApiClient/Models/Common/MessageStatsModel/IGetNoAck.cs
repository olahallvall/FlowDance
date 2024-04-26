﻿using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IGetNoAck
    {
        [JsonProperty("get_no_ack")]
        long GetNoAck { get; set; }

        [JsonProperty("get_no_ack_details")]
        RateDetails GetNoAckDetails { get; set; }
    }
}