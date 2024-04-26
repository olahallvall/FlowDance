﻿using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IGet
    {
        [JsonProperty("get")]
        long Get { get; set; }

        [JsonProperty("get_details")]
        RateDetails GetDetails { get; set; }
    }
}