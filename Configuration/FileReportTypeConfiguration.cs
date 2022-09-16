using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Reports;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public class FileReportTypeConfiguration : ReportTypeConfiguration
    {
        public override List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution)
        {
            return new List<ReportExecution>();
        }

        public override ReportProcessor GetReportProcessor(IAppConfiguration configuration, ILogger logger)
        {
            return new FileReportProcessor(configuration, logger, this);
        }
    }
}
