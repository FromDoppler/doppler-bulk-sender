using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Doppler.BulkSender.Reports
{
    public class FullDailyReportProcessor : DailyReportProcessor
    {
        public FullDailyReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution)
        {
            return new List<string>();
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            _logger.LogDebug($"Create full daily report for user {user.Name}.");

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

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields));

            DateTime start = reportExecution.LastRun.AddHours(-_reportTypeConfiguration.OffsetHour);
            DateTime end = reportExecution.NextRun.AddHours(-_reportTypeConfiguration.OffsetHour);

            List<ReportItem> items = GetReportItems("", ' ', user.Credentials.AccountId, user.UserGMT, _reportTypeConfiguration.DateFormat, start, end);

            if (items.Count == 0)
            {
                return null;
            }

            report.AppendItems(items);

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

        protected List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat, DateTime start, DateTime end)
        {
            var items = new List<ReportItem>();

            try
            {
                GetDataFromDB(items, dateFormat, userId, reportGMT, start, end);

                return items;
            }
            catch (Exception)
            {
                _logger.LogError("Error trying to get report items");
                throw;
            }
        }

        protected void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT, DateTime start, DateTime end)
        {
            var sqlHelper = new SqlHelper();

            try
            {
                List<DBStatusDto> dbReportItemList = sqlHelper.GetResultsByDeliveryDate(userId, start, end);

                foreach (DBStatusDto statusDto in dbReportItemList)
                {
                    var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                    MapDBStatusDtoToReportItem(statusDto, item, reportGMT, dateFormat);

                    items.Add(item);
                }

                sqlHelper.CloseConnection();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on get data from DB {e}");
                throw;
            }
        }
    }
}
