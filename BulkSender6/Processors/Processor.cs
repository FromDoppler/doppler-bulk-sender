using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Queues;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Doppler.BulkSender.Processors
{
    public abstract class Processor
    {
        protected readonly ILogger _logger;
        protected readonly IAppConfiguration _configuration;
        protected int _lineNumber;
        private DateTime _lastStatusDate;
        private const int STATUS_MINUTES = 5;
        private const int WAITING_CONSUMERS_TIME = 5000;
        private IBulkQueue outboundQueue;
        private FileWriter resultFileWriter;
        private FileWriter errorFileWriter;

        private int _total;
        private int _processed;
        private object _lockProcessed;
        private int _errors;

        public Processor(ILogger logger, IAppConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _lineNumber = 0;
            _lastStatusDate = DateTime.MinValue;
            _total = 0;
            _processed = 0;
            _lockProcessed = new object();
        }

        public FileProcessed DoWork(object stateInfo)
        {
            //ProcessFinished += ((ThreadStateInfo)stateInfo).Handler;

            IUserConfiguration user = ((FileToProcess)stateInfo).User;
            string fileName = ((FileToProcess)stateInfo).FileName;

            try
            {
                _logger.LogDebug($"Start to process {fileName} for User:{user.Name} in Thread:{Thread.CurrentThread.ManagedThreadId}");

                if (!ValidateCredentials(user.Credentials))
                {
                    _logger.LogError($"Error to authenticate user:{user.Name}");

                    new LoginError(_configuration).SendErrorEmail(fileName, user.Alerts);

                    return null;
                }

                SendStartProcessEmail(fileName, user);

                _total = GetTotalLines(fileName, user);

                InitStatusProcess(fileName, user);

                ProcessFile(fileName, user);

                string resultFileName = GenerateResultFile(fileName, user);
                string errorFileName = GenerateErrorFile(fileName, user);

                var ftpHelper = user.Ftp.GetFtpHelper(_logger);

                UploadErrosToFTP(errorFileName, user, ftpHelper);

                UploadResultsToFTP(resultFileName, user, ftpHelper);

                if (!string.IsNullOrEmpty(resultFileName))
                {
                    var filePathHelper = new FilePathHelper(_configuration, user.Name);

                    string processedFileName = $@"{filePathHelper.GetProcessedFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}{Constants.EXTENSION_PROCESSED}";

                    File.Move(fileName, processedFileName);

                    SendEndProcessEmail(fileName, user);
                }

                FinishStatusProcess(fileName, user);

                AddReportForFile(resultFileName, user);

                RemoveQueues(fileName, user);

                RemoveAttachments(fileName, user);

                _logger.LogDebug($"Finish processing {fileName} for User:{user.Name} in Thread:{Thread.CurrentThread.ManagedThreadId} at:{DateTime.UtcNow}");
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR GENERAL PROCESS -- {e}");
            }

            var args = new FileProcessed()
            {
                Name = user.Name,
                FileName = fileName
            };
            return args;
        }

        private void FinishStatusProcess(string fileName, IUserConfiguration userConfiguration)
        {
            if (userConfiguration.Status == null)
            {
                return;
            }

            string name = Path.GetFileNameWithoutExtension(fileName);

            var fileStatus = new FileStatus()
            {
                FileName = name,
                Finished = true,
                Total = _total,
                Processed = _processed,
                LastUpdate = DateTime.UtcNow
            };

            SaveStatusFile(fileStatus, userConfiguration);

            _lastStatusDate = DateTime.Now;
        }

        private void InitStatusProcess(string fileName, IUserConfiguration userConfiguration)
        {
            if (userConfiguration.Status == null)
            {
                return;
            }

            string name = Path.GetFileNameWithoutExtension(fileName);

            var fileStatus = new FileStatus()
            {
                FileName = name,
                Finished = false,
                Total = _total,
                Processed = 0,
                LastUpdate = DateTime.UtcNow
            };

            SaveStatusFile(fileStatus, userConfiguration);

            _lastStatusDate = DateTime.Now;
        }

        private void SaveStatusFile(FileStatus fileStatus, IUserConfiguration userConfiguration)
        {
            string jsonContent = JsonConvert.SerializeObject(fileStatus, Formatting.None);

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string statusFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{fileStatus.FileName}.status.tmp";

            var fileWriter = new FileWriter(statusFileName);

            fileWriter.WriteFile(jsonContent);
        }

        private void RemoveAttachments(string fileName, IUserConfiguration user)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string attachmentsDirectory = filePathHelper.GetAttachmentsFilesFolder(Path.GetFileNameWithoutExtension(fileName));

            if (Directory.Exists(attachmentsDirectory))
            {
                try
                {
                    Directory.Delete(attachmentsDirectory, true);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error trying to delete attachments -- {e}");
                }
            }
        }

        private void RemoveQueues(string fileName, IUserConfiguration userConfiguration)
        {
            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            try
            {
                string errorFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.error.tmp";
                if (File.Exists(errorFileName))
                {
                    File.Delete(errorFileName);
                }

                string resultFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.result.tmp";
                if (File.Exists(resultFileName))
                {
                    File.Delete(resultFileName);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error trying to delete queues for file:{fileName} -- {e}");
            }
        }

        private string GenerateResultFile(string fileName, IUserConfiguration user)
        {
            int lineNumber = 0;
            var errors = GetErrorsFromFile(user, fileName);
            var results = GetResultsFromFile(user, fileName);
            var templateConfiguration = user.GetTemplateConfiguration(fileName);

            var sent = new StringBuilder();

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line = null;

                if (templateConfiguration.HasHeaders)
                {
                    line = streamReader.ReadLine();
                    lineNumber++;
                }

                string headers = GetHeaderLine(line, templateConfiguration);
                string resultHeaders = $"{headers}{templateConfiguration.FieldSeparator}{Constants.HEADER_PROCESS_RESULT}{templateConfiguration.FieldSeparator}{Constants.HEADER_DELIVERY_RESULT}{templateConfiguration.FieldSeparator}{Constants.HEADER_MESSAGE_ID}{templateConfiguration.FieldSeparator}{Constants.HEADER_DELIVERY_LINK}";

                sent.AppendLine(resultHeaders);

                while (!streamReader.EndOfStream)
                {
                    line = streamReader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        lineNumber++;
                        continue;
                    }

                    string resultLine = string.Empty;

                    ProcessResult processResult = results.FirstOrDefault(x => x.LineNumber == lineNumber);

                    if (processResult != null)
                    {
                        resultLine = $"{processResult.GetResultLine(templateConfiguration.FieldSeparator)}";
                    }

                    ProcessError processError = errors.FirstOrDefault(x => x.LineNumber == lineNumber);

                    if (processError != null)
                    {
                        resultLine = $"{processError.GetErrorLineResult(templateConfiguration.FieldSeparator, user.Results.MaxDescriptionLength)}";
                    }

                    if (!string.IsNullOrEmpty(resultLine))
                    {
                        line = $"{line}{templateConfiguration.FieldSeparator}{resultLine}";
                        sent.AppendLine(line);
                    }

                    lineNumber++;
                }
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string resultFileName = $@"{filePathHelper.GetResultsFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}{Constants.EXTENSION_SENT}";

            using (var streamWriter = new StreamWriter(resultFileName))
            {
                streamWriter.Write(sent);
            }

            return resultFileName;
        }

        private string GenerateErrorFile(string fileName, IUserConfiguration user)
        {
            var errors = GetErrorsFromFile(user, fileName);

            if (errors.Count == 0)
            {
                return null;
            }

            string errorFileName = GetErrorsFileName(fileName, user);

            var stringBuilder = new StringBuilder();

            foreach (ProcessError processError in errors.OrderBy(x => x.LineNumber))
            {
                stringBuilder.AppendLine(processError.GetErrorLine());
            }

            using (var streamWriter = new StreamWriter(errorFileName))
            {
                streamWriter.Write(stringBuilder);
            }

            return errorFileName;
        }

        //TODO: mejorar el tiempo de espera de 5 segundos y 5 minutos.
        private void UpdateStatusFile(string fileName, IUserConfiguration user, CancellationToken cancellationToken)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.Now.Subtract(_lastStatusDate).TotalMinutes > STATUS_MINUTES)
                {
                    var fileStatus = new FileStatus()
                    {
                        FileName = name,
                        Finished = false,
                        Total = _total,
                        LastUpdate = DateTime.UtcNow
                    };

                    lock (_lockProcessed)
                    {
                        fileStatus.Processed = _processed;
                    }

                    SaveStatusFile(fileStatus, user);

                    _lastStatusDate = DateTime.Now;
                }

                Thread.Sleep(5000);
            }
        }

        private List<ProcessError> GetErrorsFromFile(IUserConfiguration userConfiguration, string fileName)
        {
            var errors = new List<ProcessError>();

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
            string errorQueue = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.error.tmp";

            if (!File.Exists(errorQueue))
            {
                return errors;
            }

            using (var fileStream = new FileStream(errorQueue, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();

                    ProcessError processError = JsonConvert.DeserializeObject<ProcessError>(line);

                    errors.Add(processError);
                }
            }

            return errors;
        }

        private List<ProcessResult> GetResultsFromFile(IUserConfiguration userConfiguration, string fileName)
        {
            var results = new List<ProcessResult>();

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
            string resultQueue = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.result.tmp";

            if (!File.Exists(resultQueue))
            {
                return results;
            }

            using (var fileStream = new FileStream(resultQueue, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();

                    ProcessResult processResult = JsonConvert.DeserializeObject<ProcessResult>(line);

                    results.Add(processResult);
                }
            }

            return results;
        }

        private void AddReportForFile(string fileName, IUserConfiguration user)
        {
            if (string.IsNullOrEmpty(fileName) || user.Reports == null)
            {
                return;
            }

            string reportFileName = $@"{_configuration.ReportsFolder}\reports.{DateTime.UtcNow.ToString("yyyyMMdd")}.json";
            List<ReportExecution> allReports = new List<ReportExecution>();

            if (File.Exists(reportFileName))
            {
                string json = File.ReadAllText(reportFileName);

                List<ReportExecution> executions = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                allReports.AddRange(executions);
            }

            ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(fileName);

            List<FileReportTypeConfiguration> reportTypes = user.Reports.ReportsList
                .OfType<FileReportTypeConfiguration>()
                .Where(x => x.Templates.Contains(templateConfiguration.TemplateName))
                .ToList();

            if (reportTypes != null && reportTypes.Count > 0)
            {
                foreach (FileReportTypeConfiguration reportType in reportTypes)
                {
                    var reportExecution = new ReportExecution()
                    {
                        UserName = user.Name,
                        ReportId = reportType.ReportId,
                        FileName = Path.GetFileName(fileName),
                        CreatedAt = DateTime.UtcNow,
                        RunDate = DateTime.UtcNow.AddHours(reportType.OffsetHour),
                        Processed = false
                    };

                    allReports.Add(reportExecution);
                }

                string reports = JsonConvert.SerializeObject(allReports);
                using (var streamWriter = new StreamWriter(reportFileName, false))
                {
                    streamWriter.Write(reports);
                }
            }
        }

        protected string GetAttachmentFile(string attachmentFile, string originalFile, IUserConfiguration user)
        {
            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder();

            //1- local file 
            string localAttachmentFile = $@"{localAttachmentFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentFile))
            {
                return localAttachmentFile;
            }

            //2- local file in subfolder
            string subFolder = Path.GetFileNameWithoutExtension(originalFile);
            string localAttachmentSubFile = $@"{localAttachmentFolder}\{subFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentSubFile))
            {
                return localAttachmentSubFile;
            }

            ITemplateConfiguration templateConfiguration = user.GetTemplateConfiguration(originalFile);

            //3- donwload from ftp
            string ftpAttachmentFile = $@"{templateConfiguration.AttachmentsFolder}/{attachmentFile}";

            var ftpHelper = user.Ftp.GetFtpHelper(_logger);
            ftpHelper.DownloadFile(ftpAttachmentFile, localAttachmentFile);

            if (File.Exists(localAttachmentFile))
            {
                ftpHelper.DeleteFile(ftpAttachmentFile);
                return localAttachmentFile;
            }

            //4- zip file 
            string zipAttachments = $@"{templateConfiguration.AttachmentsFolder}/{Path.GetFileNameWithoutExtension(originalFile)}{Constants.EXTENSION_ZIP}";
            string localZipAttachments = $@"{localAttachmentFolder}\{Path.GetFileNameWithoutExtension(originalFile)}{Constants.EXTENSION_ZIP}";

            // TODO: add retries.
            ftpHelper.DownloadFile(zipAttachments, localZipAttachments);

            if (File.Exists(localZipAttachments))
            {
                string newZipDirectory = $@"{localAttachmentFolder}\{subFolder}";
                Directory.CreateDirectory(newZipDirectory);

                var zipHelper = new ZipHelper(_logger);
                zipHelper.UnzipFile(localZipAttachments, newZipDirectory);

                ftpHelper.DeleteFile(zipAttachments);
                File.Delete(localZipAttachments); //TODO add retries.
            }

            if (File.Exists(localAttachmentSubFile))
            {
                return localAttachmentSubFile;
            }

            return null;
        }

        private void UploadResultsToFTP(string file, IUserConfiguration user, IFtpHelper ftpHelper)
        {
            if (File.Exists(file) && user.Results != null)
            {
                _logger.LogDebug($"Start to process results for file {file}");

                var filePathHelper = new FilePathHelper(_configuration, user.Name);

                string resultsPath = filePathHelper.GetResultsFilesFolder();

                string resultsFilePath = user.Results.SaveAndGetName(file, resultsPath);

                string ftpFileName = $@"{user.Results.Folder}/{Path.GetFileName(resultsFilePath)}";

                if (!File.Exists(resultsFilePath))
                {
                    resultsFilePath = file;
                }

                ftpHelper.UploadFile(resultsFilePath, ftpFileName);
            }
        }

        private void SendStartProcessEmail(string file, IUserConfiguration user)
        {
            if (user.Alerts != null
                && user.Alerts.GetStartAlert() != null
                && user.Alerts.Emails.Count > 0
                && new FileInfo(file).Directory.Name != Constants.FOLDER_RETRIES)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage();
                mailMessage.Subject = user.Alerts.GetStartAlert().Subject;
                mailMessage.From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support");

                foreach (string email in user.Alerts.Emails)
                {
                    mailMessage.To.Add(email);
                }

                string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\StartProcess.es.html");

                mailMessage.Body = body.Replace("{{filename}}", Path.GetFileNameWithoutExtension(file)).Replace("{{time}}", user.GetUserDateTime().DateTime.ToString());
                mailMessage.IsBodyHtml = true;

                try
                {
                    smtpClient.Send(mailMessage);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error trying to send starting email -- {e}");
                }
            }
        }

        private void SendEndProcessEmail(string file, IUserConfiguration user)
        {
            if (user.Alerts != null && user.Alerts.GetEndAlert() != null && user.Alerts.Emails.Count > 0)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage();
                mailMessage.Subject = user.Alerts.GetEndAlert().Subject;
                mailMessage.From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support");

                foreach (string email in user.Alerts.Emails)
                {
                    mailMessage.To.Add(email);
                }

                mailMessage.Body = GetBody(file, user, _processed, _errors);
                mailMessage.IsBodyHtml = true;

                List<string> attachments = GetAttachments(file, user.Name);

                foreach (string attachment in attachments)
                {
                    mailMessage.Attachments.Add(new Attachment(attachment)
                    {
                        Name = Path.GetFileName(attachment)
                    });
                }

                try
                {
                    smtpClient.Send(mailMessage);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error trying to send end process email -- {e}");
                }
            }
        }

        protected virtual string GetHeaderLine(string line, ITemplateConfiguration templateConfiguration)
        {
            if (templateConfiguration != null && !templateConfiguration.HasHeaders)
            {
                return string.Join(templateConfiguration.FieldSeparator.ToString(), templateConfiguration.Fields.Select(x => x.Name));
            }

            return line;
        }

        protected abstract List<string> GetAttachments(string file, string userName);

        protected abstract string GetBody(string file, IUserConfiguration user, int processedCount, int errorsCount);

        private void UploadErrosToFTP(string fileName, IUserConfiguration user, IFtpHelper ftpHelper)
        {
            if (user.Errors != null && File.Exists(fileName))
            {
                _logger.LogDebug($"Upload error file {fileName}");

                string ftpFileName = $@"{user.Errors.Folder}/{Path.GetFileName(fileName)}";

                ftpHelper.UploadFile(fileName, ftpFileName);
            }
        }

        private string GetErrorsFileName(string file, IUserConfiguration user)
        {
            string errorsFilePath = null;

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string errorsPath = filePathHelper.GetResultsFilesFolder();

            if (user.Errors != null)
            {
                errorsFilePath = $@"{errorsPath}\{user.Errors.Name.GetReportName(file, errorsPath)}";
            }
            else
            {
                errorsFilePath = $@"{errorsPath}\{Path.GetFileNameWithoutExtension(file)}.error";
            }

            return errorsFilePath;
        }

        private int GetTotalLines(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName))
            {
                return 0;
            }

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            if (templateConfiguration == null)
            {
                return 0;
            }

            int totalLines = 0;

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        totalLines++;
                    }
                }
            }

            if (templateConfiguration.HasHeaders)
            {
                totalLines--;
            }

            return totalLines;
        }

        private void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            outboundQueue = new MemoryBulkQueue();

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string errorFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.error.tmp";
            errorFileWriter = new FileWriter(errorFileName);

            string resultFileName = $@"{filePathHelper.GetQueueFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}.result.tmp";
            resultFileWriter = new FileWriter(resultFileName);

            List<ProcessError> errorList = GetErrorsFromFile(userConfiguration, fileName);
            _errors = errorList.Count;

            List<ProcessResult> resultList = GetResultsFromFile(userConfiguration, fileName);
            _processed = resultList.Count + _errors;

            IQueueProducer producer = GetProducer();

            List<IQueueConsumer> consumers = GetConsumers(userConfiguration.MaxThreadsNumber);

            var consumerCancellationTokenSource = new CancellationTokenSource();
            CancellationToken consumerCancellationToken = consumerCancellationTokenSource.Token;

            CancellationTokenSource statusCancellationTokenSource = new CancellationTokenSource();
            Task taskStatus = null;

            var tasks = new List<Task>();

            Task taskProducer = Task.Factory.StartNew(() => producer.GetMessages(userConfiguration, outboundQueue, errorList, resultList, fileName, consumerCancellationToken));
            taskProducer.ContinueWith((t) =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    consumerCancellationTokenSource.Cancel();
                }
            });
            //TO DEBUG
            //taskProducer.Wait();

            foreach (IQueueConsumer queueConsumer in consumers)
            {
                Task taskConsumer = Task.Factory.StartNew(() => queueConsumer.ProcessMessages(userConfiguration, outboundQueue, consumerCancellationToken));

                tasks.Add(taskConsumer);

                //TO DEBUG
                //taskConsumer.Wait();
            }

            if (userConfiguration.Status != null)
            {
                CancellationToken statusCancellationToken = statusCancellationTokenSource.Token;

                taskStatus = Task.Factory.StartNew(() => UpdateStatusFile(fileName, userConfiguration, statusCancellationToken));
            }

            try
            {
                taskProducer.Wait();
            }
            catch (Exception e)
            {
                _logger.LogError($"PROCESS FILE ERROR: {e}");

                new UnexpectedError(_configuration).SendErrorEmail(fileName, userConfiguration.Alerts);
            }
            finally
            {
                //TODO: mejorar esta espera de productor consumidor
                //espero que se vacie la lista y aviso con el cancel token a los consumidores.
                //while (outboundQueue.GetCount() > 0)
                //{
                //    Thread.Sleep(WAITING_CONSUMERS_TIME);
                //}

                //consumerCancellationTokenSource.Cancel();

                Task.WaitAll(tasks.ToArray());

                if (userConfiguration.Status != null)
                {
                    statusCancellationTokenSource.Cancel();

                    taskStatus.Wait();
                }
            }
        }

        protected abstract IQueueProducer GetProducer();

        protected abstract List<IQueueConsumer> GetConsumers(int count);

        protected void Processor_ErrorEvent(object sender, QueueErrorEventArgs e)
        {
            var processError = new ProcessError()
            {
                LineNumber = e.LineNumber,
                Date = e.Date,
                Message = e.Message,
                Type = e.Type,
                Description = e.Description
            };

            string text = JsonConvert.SerializeObject(processError, Formatting.None);

            errorFileWriter.AppendLine(text);

            lock (_lockProcessed)
            {
                _processed++;
                _errors++;
            }
        }

        protected void Processor_ResultEvent(object sender, QueueResultEventArgs e)
        {
            var processResult = new ProcessResult()
            {
                LineNumber = e.LineNumber,
                ResourceId = e.ResourceId,
                DeliveryLink = e.DeliveryLink,
                Message = e.Message,
                EnqueueTime = e.EnqueueTime,
                DequeueTime = e.DequeueTime,
                DeliveryTime = e.DeliveryTime
            };

            string text = JsonConvert.SerializeObject(processResult, Formatting.None);

            resultFileWriter.AppendLine(text);

            lock (_lockProcessed)
            {
                _processed++;
            }
        }

        private bool ValidateCredentials(CredentialsConfiguration credentials)
        {
            var restClient = new RestClient(_configuration.BaseUrl);

            string resource = _configuration.AccountUrl.Replace("{AccountId}", credentials.AccountId.ToString());
            var request = new RestRequest(resource, Method.Get);

            string value = $"token {credentials.ApiKey}";
            request.AddHeader("Authorization", value);

            try
            {
                RestResponse response = restClient.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    string result = response.Content;
                    _logger.LogInformation($"Validate credentials fail:{result}");
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Validate credentials error -- {e}");
                return false;
            }
        }
    }
}
