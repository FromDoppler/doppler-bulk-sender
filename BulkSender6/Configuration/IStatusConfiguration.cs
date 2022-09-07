using Doppler.BulkSender.Processors.Status;

namespace Doppler.BulkSender.Configuration
{
    public interface IStatusConfiguration
    {
        int LastViewingHours { get; set; }
        int MinutesToRefresh { get; set; }
        StatusProcessor GetStatusProcessor(ILogger logger, IAppConfiguration configuration);
    }
}