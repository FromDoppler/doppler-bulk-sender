using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors;
using Doppler.BulkSender.Classes;

namespace Doppler.BulkSender.Configuration
{
    public class TemplateGireConfiguration : BaseTemplateConfiguration
    {
        public override Processor GetProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new ApiProcessorGire(logger, configuration);
        }
    }
}
