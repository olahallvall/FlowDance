namespace FlowDance.AzureFunctions.RabbitMqHttpApiClient.Models.QueueModel
{
    public class GetQueueMessagesRequest 
    {
        public long count { get; set; }
        public bool requeue { get; set; }
        public string encoding { get; set; }
    }
}