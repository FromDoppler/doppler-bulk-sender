using System;

namespace Doppler.BulkSender.Queues
{
    public class QueueEventArgs
    {
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public DateTime EnqueueTime { get; set; }
        public DateTime DequeueTime { get; set; }
        public DateTime DeliveryTime { get; set; }
    }
}