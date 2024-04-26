using Newtonsoft.Json;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.SystemInfoModel
{
    public class ManagementPluginExtension
    {
        [JsonProperty("javascript")]
        public string Javascript { get; set; }
    }
}