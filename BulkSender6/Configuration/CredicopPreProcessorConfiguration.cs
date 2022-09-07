using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Processors.PreProcess;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public class CredicopPreProcessorConfiguration : IPreProcessorConfiguration
    {
        public List<TemplateMapping> Mappings { get; set; }

        public PreProcessor GetPreProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new CredicopPreProcessor(logger, configuration, Mappings);
        }
    }

    public class TemplateMapping
    {
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
    }
}
