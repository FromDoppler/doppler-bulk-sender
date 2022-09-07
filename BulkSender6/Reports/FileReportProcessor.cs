using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Reports
{
    public class FileReportProcessor : ReportProcessor
    {
        public FileReportProcessor(IAppConfiguration configuration, ILogger logger, ReportTypeConfiguration reportTypeConfiguration)
            : base(logger, configuration, reportTypeConfiguration)
        {

        }

        protected override List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution)
        {
            var fileList = new List<string>();
            var filePathHelper = new FilePathHelper(_configuration, user.Name);
            var directoryInfo = new DirectoryInfo(filePathHelper.GetResultsFilesFolder());

            FileInfo file = new FileInfo($@"{filePathHelper.GetResultsFilesFolder()}\{reportExecution.FileName}");

            ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(file.FullName);

            if (_reportTypeConfiguration.Templates.Contains(templateConfiguration.TemplateName))
            {
                fileList.Add(file.FullName);
            }

            return fileList;
        }

        protected override List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution)
        {
            if (files.Count == 0)
            {
                return null;
            }

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var reports = new List<string>();

            foreach (string file in files)
            {
                _logger.LogDebug($"Create report file with {file} for user {user.Name}.");

                ReportBase report = GetReport(file, filePathHelper, user);

                ITemplateConfiguration template = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                List<string> fileHeaders = GetFileHeaders(file, template.FieldSeparator);

                report.AddHeaders(GetHeadersList(_reportTypeConfiguration.ReportFields, fileHeaders));

                List<ReportItem> items = GetReportItems(file, template.FieldSeparator, user.Credentials.AccountId, user.UserGMT, _reportTypeConfiguration.DateFormat);

                report.AppendItems(items);

                string reportFileName = report.Generate();

                if (!string.IsNullOrEmpty(reportFileName) && File.Exists(reportFileName))
                {
                    reportExecution.ReportFile = Path.GetFileName(reportFileName);

                    reports.Add(reportFileName);

                    UploadFileToFtp(reportFileName, ((UserApiConfiguration)user).Reports.Folder, ftpHelper);
                }
            }

            return reports;
        }

        private List<string> GetFileHeaders(string file, char separator)
        {
            using (var streamReader = new StreamReader(file))
            {
                List<string> fileHeaders = streamReader.ReadLine().Split(separator).ToList();
                return fileHeaders;
            }
        }

        private Dictionary<int, List<string>> GetCustomItems(string sourceFile, int reportGMT)
        {
            var customItems = new Dictionary<int, List<string>>();

            if (_reportTypeConfiguration.ReportItems != null)
            {
                foreach (ReportItemConfiguration riConfiguration in _reportTypeConfiguration.ReportItems)
                {
                    customItems.Add(riConfiguration.Row, riConfiguration.Values);
                }
            }

            string sendDate = new FileInfo(sourceFile).CreationTimeUtc.AddHours(reportGMT).ToString("yyyyMMdd");
            var customValues = new List<List<string>>()
            {
                new List<string>() { "Información de envio" },
                new List<string>() { "Nombre", Path.GetFileNameWithoutExtension(sourceFile) },
                new List<string>() { "Fecha de envio", sendDate },
                new List<string>() { "Detalle suscriptores" }
            };
            int index = 0;
            foreach (List<string> value in customValues)
            {
                while (customItems.ContainsKey(index))
                {
                    index++;
                }
                customItems.Add(index, value);
                index++;
            }

            return customItems;
        }

        protected List<ReportItem> GetReportItems(string file, char separator, int userId, int reportGMT, string dateFormat)
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

        protected virtual ReportBase GetReport(string file, FilePathHelper filePathHelper, IUserConfiguration user)
        {
            var report = new ExcelReport()
            {
                ReportName = _reportTypeConfiguration.Name.GetReportName(Path.GetFileName(file), filePathHelper.GetReportsFilesFolder()),
                CustomItems = GetCustomItems(file, user.UserGMT),
                ReportPath = filePathHelper.GetReportsFilesFolder(),
                ReportGMT = user.UserGMT,
                UserId = user.Credentials.AccountId
            };

            return report;
        }
    }
}
