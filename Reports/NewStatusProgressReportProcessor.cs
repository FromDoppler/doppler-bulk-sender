using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Doppler.BulkSender.Reports
{
    public class NewStatusProgressReportProcessor : ReportProcessor
    {
        private const int MAX_HOUR_RANGE = 2;
        private const int MAX_REPORT_ITEMS = 100000;
        private ReportBuffer buffer;

        public NewStatusProgressReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution)
        {
            return new List<string>();
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            _logger.LogDebug($"Process status progress report for user {user.Name}.");

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new CsvSplittedReport()
            {
                Separator = _reportTypeConfiguration.FieldSeparator,
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportName = _reportTypeConfiguration.Name.GetReportName(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields));

            List<ReportItem> items = GetReportItems(user, _reportTypeConfiguration.DateFormat, reportExecution.NextRun, _reportTypeConfiguration.RunHour, _reportTypeConfiguration.OffsetHour);

            DBStatusDto dbReportItem;

            List<string> reports = new List<string>();

            while ((dbReportItem = buffer.Read()) != null)
            {
                var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                MapDBStatusDtoToReportItem(dbReportItem, item, user.UserGMT, _reportTypeConfiguration.DateFormat);

                items.Add(item);

                if (items.Count == MAX_REPORT_ITEMS)
                {
                    report.AppendItems(items);

                    string name = report.SplitGenerate();

                    reports.Add(name);

                    items.Clear();
                }
            }

            if (items.Count > 0)
            {
                report.AppendItems(items);

                string name = report.SplitGenerate();

                reports.Add(name);

                items.Clear();
            }

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);

            foreach (string reportFileName in reports)
            {
                if (File.Exists(reportFileName))
                {
                    reportExecution.ReportFile = string.Join("|", reports.Select(x => Path.GetFileName(x)));

                    UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
                }
            }

            return reports;
        }

        protected List<ReportItem> GetReportItems(IUserConfiguration user, string dateFormat, DateTime end, int range, int offset)
        {
            var items = new List<ReportItem>();

            try
            {
                buffer = new ReportBuffer(_configuration, user, end, range, offset);

                GetDataFromDB(items, dateFormat, user.Credentials.AccountId, user.UserGMT, buffer.GetStartDate(), buffer.GetEndDate());

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

            DateTime from, to;

            try
            {
                while (start < end)
                {
                    from = start;
                    to = from.AddHours(MAX_HOUR_RANGE);

                    List<DBStatusDto> dbReportItemList = sqlHelper.GetResultsByDeliveryDate(userId, from, to);

                    buffer.AddItemsOrder(dbReportItemList);

                    start = start.AddHours(MAX_HOUR_RANGE);
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