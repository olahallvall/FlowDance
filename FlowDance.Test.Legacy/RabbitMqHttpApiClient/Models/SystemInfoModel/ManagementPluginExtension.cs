using Newtonsoft.Json;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.SystemInfoModel
{
    public class ManagementPluginExtension
    {
        [JsonProperty("javascript")]
        public string Javascript { get; set; }
    }
}