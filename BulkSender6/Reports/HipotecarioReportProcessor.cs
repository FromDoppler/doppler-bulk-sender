using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Doppler.BulkSender.Reports
{
    public abstract class HipotecarioReportProcessor : DailyReportProcessor
    {
        public HipotecarioReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            if (files.Count == 0)
            {
                return null;
            }

            _logger.LogDebug($"Create Detail Report for user {user.Name}.");

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new ZipCsvReport()
            {
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields));

            foreach (string file in files)
            {
                ITemplateConfiguration template = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                List<ReportItem> items = GetReportItems(file, template.FieldSeparator, user.Credentials.AccountId, user.UserGMT, _reportTypeConfiguration.DateFormat);

                report.AppendItems(items);
            }

            string reportFileName = report.Generate();

            var reports = new List<string>();

            if (File.Exists(reportFileName))
            {
                reports.Add(reportFileName);

                reportExecution.ReportFile = Path.GetFileName(reportFileName);

                var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
            }

            return reports;
        }
    }
}