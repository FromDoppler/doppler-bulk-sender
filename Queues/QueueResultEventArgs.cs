namespace Doppler.BulkSender.Queues
{
    public class QueueResultEventArgs : QueueEventArgs
    {
        public string ResourceId { get; set; }
        public string DeliveryLink { get; set; }
    }
}