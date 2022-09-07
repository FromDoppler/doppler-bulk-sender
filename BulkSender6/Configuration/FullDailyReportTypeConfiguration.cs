using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Reports;

namespace Doppler.BulkSender.Configuration
{
    public class FullDailyReportTypeConfiguration : DailyReportTypeConfiguration
    {
        public override ReportProcessor GetReportProcessor(IAppConfiguration configuration, ILogger logger)
        {
            return new FullDailyReportProcessor(logger, configuration, this);
        }
    }
}
