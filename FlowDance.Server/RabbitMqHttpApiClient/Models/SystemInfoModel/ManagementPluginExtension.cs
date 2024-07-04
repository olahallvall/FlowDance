using Newtonsoft.Json;

namespace FlowDance.Server.RabbitMqHttpApiClient.Models.SystemInfoModel
{
    public class ManagementPluginExtension
    {
        [JsonProperty("javascript")]
        public string Javascript { get; set; }
    }
}