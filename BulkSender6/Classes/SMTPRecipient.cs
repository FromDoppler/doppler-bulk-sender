using System.Collections.Generic;

namespace Doppler.BulkSender.Classes
{
    public class SMTPRecipient : Recipient
    {
        public string Body { get; set; }
        public List<string> Attachments { get; set; }
    }
}
