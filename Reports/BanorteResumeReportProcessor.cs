using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Doppler.BulkSender.Reports
{
    public class BanorteResumeReportProcessor : DailyReportProcessor
    {
        public BanorteResumeReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            if (files.Count == 0)
            {
                return null;
            }

            _logger.LogDebug($"Create resume report for user {user.Name}.");

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new TemplateReport()
            {
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportPath = filePathHelper.GetReportsFilesFolder(),
            };

            foreach (string file in files)
            {
                ITemplateConfiguration template = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                List<ReportItem> items = GetReportItems(file, template.FieldSeparator, user.Credentials.AccountId, user.UserGMT, "dd/MM/yyyy HH:mm");

                report.AppendItems(items);
            }

            string reportFileName = report.Generate();

            var reports = new List<string>();

            if (File.Exists(reportFileName))
            {
                reports.Add(reportFileName);

                reportExecution.ReportFile = Path.GetFileName(reportFileName);
            }

            return reports;
        }

        protected override void SendReportAlert(IUserConfiguration user, List<string> files)
        {
            if (user.Alerts != null
                && user.Alerts.GetReportAlert() != null
                && user.Alerts.Emails.Count > 0
                && files != null
                && files.Count > 0)
            {
                SendSmtpEmail(user.Alerts.Emails, user.Alerts.GetReportAlert().Subject, File.ReadAllText(files[0]), new List<string>());
            }
        }

        protected override List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat)
        {
            var items = new List<ReportItem>();
            int processed, errors;
            processed = errors = 0;

            try
            {
                using (var streamReader = new StreamReader(file))
                {
                    List<string> fileHeaders = streamReader.ReadLine().Split(separator).ToList();

                    List<ReportFieldConfiguration> reportHeaders = GetHeadersIndexes(_reportTypeConfiguration.ReportFields, fileHeaders, out int processedIndex, out int resultIndex);

                    if (processedIndex == -1 || resultIndex == -1)
                    {
                        return items;
                    }

                    while (!streamReader.EndOfStream)
                    {
                        string[] lineArray = streamReader.ReadLine().Split(separator);

                        processed++;

                        if (lineArray.Length <= resultIndex || lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
                        {
                            errors++;
                        }
                    }
                }

                var fileInfo = new FileInfo(file);

                var reportItem = new ReportItem(4);

                reportItem.AddValue(fileInfo.Name, 0);
                reportItem.AddValue(fileInfo.LastWriteTimeUtc.ToString(), 1);
                reportItem.AddValue(processed.ToString(), 2);
                reportItem.AddValue(errors.ToString(), 3);

                items.Add(reportItem);

                return items;
            }
            catch (Exception)
            {
                _logger.LogError("Error trying to get report items");
                throw;
            }
        }
    }
}
