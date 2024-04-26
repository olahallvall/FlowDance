using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.SystemInfoModel
{
    public class ManagementPluginExtension
    {
        [JsonProperty("javascript")]
        public string Javascript { get; set; }
    }
}