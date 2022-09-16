using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors;
using Doppler.BulkSender.Classes;

namespace Doppler.BulkSender.Configuration
{
    public class TemplateKeyIteratorConfiguration : BaseTemplateConfiguration
    {
        public override Processor GetProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new APIProcessorKeyIterator(logger, configuration);
        }
    }
}
