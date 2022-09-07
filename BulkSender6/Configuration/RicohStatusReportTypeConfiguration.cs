using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Reports;

namespace Doppler.BulkSender.Configuration
{
    public class RicohStatusReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportProcessor GetReportProcessor(IAppConfiguration configuration, ILogger logger)
        {
            return new RicohStatusReportProcessor(logger, configuration, this);
        }
    }
}
