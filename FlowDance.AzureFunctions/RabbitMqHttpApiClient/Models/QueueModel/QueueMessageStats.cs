using FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.Common;
using FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.Common.MessageStatsModel;

namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.QueueModel
{
    public class QueueMessageStats : MessageStats, IDiskInfo
    {
        public long DiskReads { get; set; }
        public RateDetails DiskReadsDetails { get; set; }

        public long DiskWrites { get; set; }
        public RateDetails DiskWritesDetails { get; set; }
    }
}