using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Processors.PreProcess;

namespace Doppler.BulkSender.Configuration
{
    public class HipotecarioPreProcessorConfiguration : IPreProcessorConfiguration
    {
        public PreProcessor GetPreProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new HipotecarioPreProcessor(logger, configuration);
        }
    }
}
