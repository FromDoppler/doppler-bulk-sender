using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Reports;
using System;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public class EdesurReportTypeConfiguration : ReportTypeConfiguration
    {
        public override List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution)
        {
            if (lastExecution != null)
            {
                lastExecution.LastRun = lastExecution.NextRun;
                lastExecution.NextRun = lastExecution.NextRun.AddHours(3);
            }
            else
            {
                DateTime nextRun = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddHours(3);

                lastExecution = new ReportExecution()
                {
                    UserName = user.Name,
                    ReportId = this.ReportId,
                    NextRun = nextRun,
                    LastRun = nextRun.AddHours(-3),
                    CreatedAt = DateTime.UtcNow
                };
            }

            return new List<ReportExecution>() { lastExecution };
        }

        public override ReportProcessor GetReportProcessor(IAppConfiguration configuration, ILogger logger)
        {
            return new EdesurReportProcessor(configuration, logger, this);
        }
    }
}
