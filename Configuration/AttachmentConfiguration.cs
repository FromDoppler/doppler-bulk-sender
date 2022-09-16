using Doppler.BulkSender.Configuration;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public class AttachmentConfiguration
    {
        public string Folder { get; set; }
        public List<FieldConfiguration> Fields { get; set; }
    }
}
