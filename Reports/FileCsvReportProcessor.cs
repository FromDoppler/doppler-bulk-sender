using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.IO;

namespace Doppler.BulkSender.Reports
{
    public class CsvFileReportProcessor : FileReportProcessor
    {
        public CsvFileReportProcessor(IAppConfiguration configuration, ILogger logger, ReportTypeConfiguration reportTypeConfiguration)
            : base(configuration, logger, reportTypeConfiguration)
        {

        }

        protected override ReportBase GetReport(string file, FilePathHelper filePathHelper, IUserConfiguration user)
        {
            var report = new CsvReport()
            {
                ReportName = _reportTypeConfiguration.Name.GetReportName(Path.GetFileName(file), filePathHelper.GetReportsFilesFolder()),
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId,
                Separator = _reportTypeConfiguration.FieldSeparator
            };

            return report;
        }
    }
}
