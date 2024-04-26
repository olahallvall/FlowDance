using FlowDance.Tests.RabbitMqHttpApiClient.Models.Common;
using FlowDance.Tests.RabbitMqHttpApiClient.Models.Common.MessageStatsModel;

namespace FlowDance.Tests.RabbitMqHttpApiClient.Models.QueueModel
{
    public class QueueMessageStats : MessageStats, IDiskInfo
    {
        public long DiskReads { get; set; }
        public RateDetails DiskReadsDetails { get; set; }

        public long DiskWrites { get; set; }
        public RateDetails DiskWritesDetails { get; set; }
    }
}