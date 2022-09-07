using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors;
using Doppler.BulkSender.Classes;

namespace Doppler.BulkSender.Configuration
{
    public class ProvinciaTemplateConfiguration : BaseTemplateConfiguration
    {
        public override Processor GetProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new SMTPProcessorProvincia(logger, configuration);
        }
    }
}
