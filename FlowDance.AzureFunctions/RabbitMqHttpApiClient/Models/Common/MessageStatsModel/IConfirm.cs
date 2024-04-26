using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.Common.MessageStatsModel
{
    public interface IConfirm
    {
        [JsonProperty("confirm")]
        long Confirm { get; set; }

        [JsonProperty("confirm_details")]
        RateDetails ConfirmDetails { get; set; }
    }
}