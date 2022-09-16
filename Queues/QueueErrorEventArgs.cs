using Doppler.BulkSender.Processors.Errors;
using System;

namespace Doppler.BulkSender.Queues
{
    public class QueueErrorEventArgs : QueueEventArgs
    {
        public ErrorType Type { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}