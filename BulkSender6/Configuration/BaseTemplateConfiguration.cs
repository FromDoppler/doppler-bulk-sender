using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors;
using Doppler.BulkSender.Classes;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public abstract class BaseTemplateConfiguration : ITemplateConfiguration
    {
        public List<string> DownloadFolders { get; set; }
        public string AttachmentsFolder { get; set; }
        public string HostedFolder { get; set; }
        public char FieldSeparator { get; set; }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public bool HasHeaders { get; set; }
        public List<string> FileNameParts { get; set; }
        public List<FieldConfiguration> Fields { get; set; }
        public bool AllowDuplicates { get; set; }
        public IPreProcessorConfiguration PreProcessor { get; set; }

        public abstract Processor GetProcessor(ILogger logger, IAppConfiguration configuration);
    }
}
