using Doppler.BulkSender.Processors.Status;

namespace Doppler.BulkSender.Configuration
{
    public class FtpStatusConfiguration : IStatusConfiguration
    {
        public int LastViewingHours { get; set; }
        public int MinutesToRefresh { get; set; }
        public string FtpFolder { get; set; }
        public string StatusFileDateFormat { get; set; }

        public StatusProcessor GetStatusProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new FtpStatusProcessor(logger, configuration);
        }
    }
}