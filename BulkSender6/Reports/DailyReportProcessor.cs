using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Doppler.BulkSender.Reports
{
    public class DailyReportProcessor : ReportProcessor
    {
        public DailyReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetResultsFilesFolder());

            DateTime start = reportExecution.LastRun.AddHours(-_reportTypeConfiguration.OffsetHour);
            DateTime end = reportExecution.NextRun.AddHours(-_reportTypeConfiguration.OffsetHour);
            string searchPattern = $"*{Constants.EXTENSION_SENT}";

            var fileInfoList = directoryInfo.GetFiles(searchPattern)
                .Where(f => f.LastWriteTimeUtc >= start && f.LastWriteTimeUtc < end)
                .OrderBy(f => f.CreationTimeUtc);

            return FilterFilesByTemplate(fileInfoList.Select(x => x.FullName).ToList(), user);
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            if (files.Count == 0)
            {
                return null;
            }

            _logger.LogDebug($"Create daily report for user {user.Name}.");

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new CsvReport()
            {
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields, null));

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

                UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
            }

            return reports;
        }

        protected virtual List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat)
        {
            var items = new List<ReportItem>();

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

                        if (lineArray.Length <= resultIndex || lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
                        {
                            continue;
                        }

                        var item = new ReportItem(reportHeaders.Count);

                        foreach (ReportFieldConfiguration reportFieldConfiguration in reportHeaders.Where(x => !string.IsNullOrEmpty(x.NameInFile)))
                        {
                            item.AddValue(lineArray[reportFieldConfiguration.PositionInFile].Trim(), reportFieldConfiguration.Position);
                        }

                        item.ResultId = lineArray[resultIndex];

                        items.Add(item);
                    }
                }

                GetDataFromDB(items, dateFormat, userId, reportGMT);

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
