using Doppler.BulkSender.Processors.PreProcess;

namespace Doppler.BulkSender.Configuration
{
    public interface IPreProcessorConfiguration
    {
        PreProcessor GetPreProcessor(ILogger logger, IAppConfiguration configuration);
    }
}
