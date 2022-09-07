using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Doppler.BulkSender.Reports
{
    public class StatusProgressReportProcessor : ReportProcessor
    {
        private const int MAX_HOUR_RANGE = 6;
        private Dictionary<long, DBStatusDto> tempReport;

        public StatusProgressReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
            tempReport = LoadTempReport();
        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution)
        {
            return new List<string>();
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            _logger.LogDebug($"Process status progress report for user {user.Name}.");

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var report = new SplitCsvReport()
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

            List<string> reports = report.SplitGenerate();

            foreach (string reportFileName in reports)
            {
                if (File.Exists(reportFileName))
                {
                    reportExecution.ReportFile = string.Join("|", reports.Select(x => Path.GetFileName(x)));

                    var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                    UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
                }
            }

            return reports;
        }

        protected List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat, DateTime start, DateTime end)
        {
            var items = new List<ReportItem>();

            try
            {
                if (tempReport == null)
                {
                    start = end.AddHours(-_reportTypeConfiguration.OffsetHour);
                    tempReport = new Dictionary<long, DBStatusDto>();
                }

                GetDataFromDB(items, dateFormat, userId, reportGMT, start, end);

                start = end.AddHours(-_reportTypeConfiguration.OffsetHour);
                var keysToRemove = new List<int>();

                foreach (int key in tempReport.Keys)
                {
                    DBStatusDto dbReportItem = tempReport[key];

                    if (dbReportItem.CreatedAt >= start)
                    {
                        var item = new ReportItem(_reportTypeConfiguration.ReportFields.Count);

                        MapDBStatusDtoToReportItem(dbReportItem, item, reportGMT, dateFormat);

                        items.Add(item);
                    }
                    else
                    {
                        keysToRemove.Add(key);
                    }
                }

                ClearTempReport(keysToRemove);
                SaveTempReport();

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
            from = start;
            to = start.AddHours(MAX_HOUR_RANGE);

            try
            {
                while (from < to)
                {
                    List<DBStatusDto> dbReportItemList = sqlHelper.GetResultsByDeliveryDate(userId, from, to);

                    foreach (DBStatusDto statusDto in dbReportItemList)
                    {
                        if (tempReport.ContainsKey(statusDto.DeliveryId))
                        {
                            tempReport[statusDto.DeliveryId] = statusDto;
                        }
                        else
                        {
                            tempReport.Add(statusDto.DeliveryId, statusDto);
                        }
                    }

                    from = to;
                    to = to.AddHours(MAX_HOUR_RANGE);
                    if (to > end)
                    {
                        to = end;
                    }
                }

                sqlHelper.CloseConnection();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on get data from DB {e}");
                throw;
            }
        }

        private Dictionary<long, DBStatusDto> LoadTempReport()
        {
            string fileName = $@"{_configuration.ReportsFolder}\{_reportTypeConfiguration.ReportId}.tmp";

            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream fileStream = File.OpenRead(fileName))
                    {
                        return new BinaryFormatter().Deserialize(fileStream) as Dictionary<long, DBStatusDto>;
                    }
                }
                catch (Exception e)
                {
                    //si esta dañado lo borro y se genera de nuevo.
                    File.Delete(fileName);
                }
            }

            return null;
        }

        private void SaveTempReport()
        {
            string fileName = $@"{_configuration.ReportsFolder}\{_reportTypeConfiguration.ReportId}.tmp";

            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                new BinaryFormatter().Serialize(fileStream, tempReport);
            }
        }

        private void ClearTempReport(List<int> keysToRemove)
        {
            foreach (int key in keysToRemove)
            {
                tempReport.Remove(key);
            }
        }
    }
}