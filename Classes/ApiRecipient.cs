using Doppler.BulkSender.Queues;
using System;
using System.Collections.Generic;

namespace Doppler.BulkSender.Classes
{
    public class ApiRecipient : Recipient, IBulkQueueMessage
    {
        public string TemplateId { get; set; }
        public Dictionary<string, object> Fields { get; set; }
        public string Key { get; set; }
        public string Message { get; set; }
        public DateTime EnqueueTime { get; set; }
        public DateTime DequeueTime { get; set; }
        public List<string> Attachments { get; set; }

        public ApiRecipient()
        {
            HasError = false;
            ToEmail = null;
            ToName = null;
            CCEmail = null;
            BCCEmail = null;
            FromEmail = null;
            FromName = null;
            ReplyToEmail = null;
            ReplyToName = null;
            Fields = new Dictionary<string, object>();
            Attachments = new List<string>();
        }
    }
}
