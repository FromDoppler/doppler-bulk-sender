using Doppler.BulkSender.Processors.PreProcess;

namespace Doppler.BulkSender.Configuration
{
    public class BasicPreProcessorConfiguration : IPreProcessorConfiguration
    {
        public PreProcessor GetPreProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new BasicPreProcessor(logger, configuration);
        }
    }
}
