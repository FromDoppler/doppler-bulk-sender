using Doppler.BulkSender.Processors.Acknowledgement;

namespace Doppler.BulkSender.Configuration
{
    public interface IAckConfiguration
    {
        List<string> Extensions { get; set; }

        IAckProcessor GetAckProcessor(ILogger logger, IAppConfiguration configuration);
    }
}