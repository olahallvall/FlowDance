﻿using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.SystemInfoModel
{
    public class HealthCheck
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("reason")]
        public string ErrorReason { get; set; }

        public bool IsOk => Status == "ok";
    }
}