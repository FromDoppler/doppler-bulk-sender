using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Reports
{
    public class HipotecarioDetailReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioDetailReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }
    }
}
