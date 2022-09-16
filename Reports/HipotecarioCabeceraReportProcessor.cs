using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Doppler.BulkSender.Reports
{
    public class HipotecarioCabeceraReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioCabeceraReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            if (files.Count == 0)
            {
                return null;
            }

            _logger.LogDebug($"Process cabecera report for user {user.Name}.");

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

            List<ReportItem> items = GetReportItems("", ' ', user.Credentials.AccountId, user.UserGMT, _reportTypeConfiguration.DateFormat, reportExecution.LastRun, reportExecution.NextRun);

            report.AppendItems(items);

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
                List<DBSummarizedDto> dbReportItemList = sqlHelper.GetSummarizedByDate(userId, start, end);

                foreach (DBSummarizedDto dbReportItem in dbReportItemList)
                {
                    var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                    MapDBSummarizedDtoToReportItem(dbReportItem, item, reportGMT, dateFormat);

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
