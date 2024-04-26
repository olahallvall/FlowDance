using FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.Common;
using FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.Common.MessageStatsModel;

namespace FlowDance.Test.Legacy.RabbitMqHttpApiClient.Models.QueueModel
{
    public class QueueMessageStats : MessageStats, IDiskInfo
    {
        public long DiskReads { get; set; }
        public RateDetails DiskReadsDetails { get; set; }

        public long DiskWrites { get; set; }
        public RateDetails DiskWritesDetails { get; set; }
    }
}