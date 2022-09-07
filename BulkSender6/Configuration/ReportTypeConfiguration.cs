using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Reports;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public abstract class ReportTypeConfiguration
    {
        public string ReportId { get; set; }
        public int OffsetHour { get; set; }
        public int RunHour { get; set; }
        public IReportName Name { get; set; }
        public char FieldSeparator { get; set; }
        public List<string> Templates { get; set; }
        public string DateFormat { get; set; }
        public List<ReportItemConfiguration> ReportItems { get; set; }
        public List<ReportFieldConfiguration> ReportFields { get; set; }

        public abstract ReportProcessor GetReportProcessor(IAppConfiguration configuration, ILogger logger);

        public abstract List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution);
    }
}
