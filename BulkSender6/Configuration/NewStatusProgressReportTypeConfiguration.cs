using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Reports;
using System;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public class NewStatusProgressReportTypeConfiguration : ReportTypeConfiguration
    {
        public override List<ReportExecution> GetReportExecution(IUserConfiguration user, ReportExecution lastExecution)
        {
            var reports = new List<ReportExecution>();
            int defaultHour = 4; // esto se puede sacar del config

            DateTime nextRun, lastRun;

            if (lastExecution != null)
            {
                lastRun = lastExecution.NextRun;
                nextRun = lastExecution.NextRun.AddHours(this.RunHour);
            }
            else
            {
                DateTime today = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, defaultHour, 0, 0);

                nextRun = today;
                lastRun = nextRun.AddHours(-this.RunHour);
            }

            DateTime nextDay = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddDays(1);

            var execution = new ReportExecution()
            {
                UserName = user.Name,
                ReportId = this.ReportId,
                NextRun = nextRun,
                LastRun = lastRun,
                Processed = false,
                RunDate = nextRun,
                CreatedAt = DateTime.UtcNow,
            };

            while (execution.NextRun < nextDay)
            {
                reports.Add(execution);

                nextRun = execution.NextRun.AddHours(this.RunHour);
                lastRun = execution.NextRun;

                execution = new ReportExecution()
                {
                    UserName = user.Name,
                    ReportId = this.ReportId,
                    NextRun = nextRun,
                    LastRun = lastRun,
                    Processed = false,
                    RunDate = nextRun,
                    CreatedAt = DateTime.UtcNow
                };
            }

            return reports;
        }

        public override ReportProcessor GetReportProcessor(IAppConfiguration configuration, ILogger logger)
        {
            return new NewStatusProgressReportProcessor(logger, configuration, this);
        }
    }
}
