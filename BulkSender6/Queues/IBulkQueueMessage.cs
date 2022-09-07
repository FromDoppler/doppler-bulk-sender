using System;

namespace Doppler.BulkSender.Queues
{
    public interface IBulkQueueMessage
    {
        int LineNumber { get; set; }
        string TemplateId { get; set; }
        string Message { get; set; }
        DateTime EnqueueTime { get; set; }
        DateTime DequeueTime { get; set; }
    }
}
