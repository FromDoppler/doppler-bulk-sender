using System;

namespace Doppler.BulkSender.Queues
{
    public class BulkQueueMessage : IBulkQueueMessage
    {
        public int LineNumber { get; set; }
        public string TemplateId { get; set; }
        public string Message { get; set; }
        public DateTime EnqueueTime { get; set; }
        public DateTime DequeueTime { get; set; }
    }
}
