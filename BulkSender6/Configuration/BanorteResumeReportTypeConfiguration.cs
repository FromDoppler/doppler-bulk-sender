using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Reports;
using System;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public class BanorteResumeReportTypeConfiguration : ReportTypeConfiguration
    {
        public override List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution)
        {
            DateTime now = DateTime.UtcNow.AddHours(user.UserGMT);

            DateTime nextRun = new DateTime(now.Year, now.Month, now.Day, this.RunHour, 0, 0);

            if (nextRun < now)
            {
                nextRun = nextRun.AddDays(1);
            }

            nextRun = nextRun.AddHours(-user.UserGMT);

            var reportExecution = new ReportExecution()
            {
                UserName = user.Name,
                ReportId = this.ReportId,
                NextRun = nextRun,
                LastRun = nextRun.AddDays(-1),
                RunDate = nextRun,
                Processed = false,
                CreatedAt = DateTime.UtcNow
            };

            return new List<ReportExecution>() { reportExecution };
        }

        public override ReportProcessor GetReportProcessor(IAppConfiguration configuration, ILogger logger)
        {
            return new BanorteResumeReportProcessor(logger, configuration, this);
        }
    }
}
