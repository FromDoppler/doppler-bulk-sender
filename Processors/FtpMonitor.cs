using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.Acknowledgement;
using Doppler.BulkSender.Processors.Errors;
using Microsoft.Extensions.Options;

namespace Doppler.BulkSender.Processors
{
    public class FtpMonitor : BaseWorker
    {
        private const int MINUTES_TO_WRITE_DOWNLOAD = 5;
        private const int MINUTES_TO_DELETE_REPEATED = 60;
        private Dictionary<string, DateTime> _nextRun;
        private Dictionary<string, List<RepeatedFile>> _repeatedFiles;
        private Dictionary<string, DateTime> _alerts;
        private readonly object _lockRepeatedFiles;

        public FtpMonitor(ILogger<FtpMonitor> logger, IOptions<AppConfiguration> configuration) : base(logger, configuration)
        {
            _nextRun = new Dictionary<string, DateTime>();
            _repeatedFiles = new Dictionary<string, List<RepeatedFile>>();
            _alerts = new Dictionary<string, DateTime>();
            _lockRepeatedFiles = new object();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CheckConfigChanges();

                    foreach (IUserConfiguration user in _users)
                    {
                        if (!IsValidInterval(user.Name))
                        {
                            continue;
                        }

                        List<string> downloadFolders = user.Templates.SelectMany(x => x.DownloadFolders).Distinct().ToList();

                        ProcessAckFiles(user, downloadFolders);

                        IEnumerable<string> extensions = user.FileExtensions != null ? user.FileExtensions : new List<string> { ".csv" };

                        foreach (string folder in downloadFolders)
                        {
                            List<string> files = GetFileListWithRetries(user, folder, extensions);

                            Task.Factory.StartNew(() => DownloadUserFiles(folder, files, user));

                            //TO DEBUG
                            //Task t = Task.Factory.StartNew(() => DownloadUserFiles(folder, files, user));
                            //t.Wait();
                        }

                        RemoveRepeatedFiles(user);

                        SetNextRun(user.Name, user.FtpInterval);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GENERAL FTP MONITOR ERROR: {ex}");
                }

                await Task.Delay(_configuration.FtpListInterval, stoppingToken);
            }
        }

        private List<string> GetFileListWithRetries(IUserConfiguration user, string folder, IEnumerable<string> extensions)
        {
            var ftpHelper = user.Ftp.GetFtpHelper(_logger);

            int i = 0;

            while (i < 3)
            {
                Thread.Sleep(i * 5000);

                try
                {
                    return ftpHelper.GetFileList(folder, extensions);
                }
                catch (Exception e)
                {
                    _logger.LogError($"FTP ERROR: problems retrieving files list for user {user.Name} -- {e}");

                    i++;
                }
            }

            if (!_alerts.ContainsKey(user.Name))
            {
                _alerts.Add(user.Name, DateTime.UtcNow);

                new AdminError(_configuration, user.Name).Process();
            }
            else if (DateTime.UtcNow.Subtract(_alerts[user.Name]).TotalMinutes > 60)
            {
                _alerts[user.Name] = DateTime.UtcNow;

                new AdminError(_configuration, user.Name).Process();
            }

            return new List<string>();
        }

        private void ProcessAckFiles(IUserConfiguration userConfiguration, List<string> folders)
        {
            if (userConfiguration.Ack == null)
            {
                return;
            }

            var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);

            IAckProcessor ackProcessor = userConfiguration.GetAckProcessor(_logger, _configuration);

            foreach (string folder in folders)
            {
                List<string> ackFiles = ftpHelper.GetFileList(folder, userConfiguration.Ack.Extensions);

                foreach (string ackFile in ackFiles)
                {
                    string ftpFileName = $"{folder}/{ackFile}";
                    string localFileName = $@"{new FilePathHelper(_configuration, userConfiguration.Name).GetDownloadsFolder()}\{ackFile}";

                    if (ftpHelper.DownloadFileWithResume(ftpFileName, localFileName))
                    {
                        ackProcessor.ProcessAckFile(localFileName);

                        RemoveFileFromFtp(ftpFileName, userConfiguration);
                    }
                }
            }
        }

        private void DownloadUserFiles(string folder, List<string> files, IUserConfiguration user)
        {
            if (files.Count == 0)
            {
                return;
            }

            int parallelProcessors = user.MaxParallelProcessors != 0 ? user.MaxParallelProcessors : _configuration.MaxNumberOfThreads;

            int totalFiles = parallelProcessors * 2;

            string downloadFolder = new FilePathHelper(_configuration, user.Name).GetDownloadsFolder();

            foreach (string file in files)
            {
                if (Directory.GetFiles(downloadFolder).Length >= totalFiles)
                {
                    break;
                }

                string ftpFileName = $"{folder}/{file}";

                if (GetFileFromFTP(ftpFileName, user))
                {
                    RemoveFileFromFtp(ftpFileName, user);
                }
            }
        }

        private void SetNextRun(string name, int ftpInterval)
        {
            if (ftpInterval > 0 && ftpInterval > (_configuration.FtpListInterval / 60000))
            {
                if (_nextRun.ContainsKey(name))
                {
                    _nextRun[name] = DateTime.UtcNow.AddMinutes(ftpInterval);
                }
                else
                {
                    _nextRun.Add(name, DateTime.UtcNow.AddMinutes(ftpInterval));
                }
            }
            else
            {
                if (_nextRun.ContainsKey(name))
                {
                    _nextRun.Remove(name);
                }
            }
        }

        private bool IsValidInterval(string name)
        {
            if (_nextRun.ContainsKey(name) && _nextRun[name] > DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        private bool GetFileFromFTP(string file, IUserConfiguration user)
        {
            string localFileName;

            if (!ValidateFile(file, user, out localFileName))
            {
                return false;
            }

            _logger.LogDebug($"Start to download {file} for user {user.Name}");

            IFtpHelper ftpHelper = user.Ftp.GetFtpHelper(_logger);

            bool downloadResult = ftpHelper.DownloadFileWithResume(file, localFileName);

            if (downloadResult && File.Exists(localFileName))
            {
                string newFileName = $@"{new FilePathHelper(_configuration, user.Name).GetDownloadsFolder()}\{Path.GetFileName(file)}";

                File.Move(localFileName, newFileName);
            }
            else
            {
                new DownloadError(_configuration).SendErrorEmail(file, user.Alerts);

                _logger.LogError($"Download problems with file {file}.");

                return false;
            }

            return true;
        }

        private bool ValidateFile(string file, IUserConfiguration userConfiguration, out string localFileName)
        {
            string fileName = Path.GetFileName(file);
            string name = Path.GetFileNameWithoutExtension(fileName);

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string downloadPath = filePathHelper.GetDownloadsFolder();
            string processedPath = filePathHelper.GetProcessedFilesFolder();
            string retryPath = filePathHelper.GetRetriesFilesFolder();

            localFileName = $@"{downloadPath}\{name}{Constants.EXTENSION_DOWNLOADING}";

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(file);

            bool allowDuplicates = templateConfiguration != null && templateConfiguration.AllowDuplicates ? true : false;

            var fileInfo = new FileInfo(localFileName);

            if (fileInfo.Exists && DateTime.UtcNow.Subtract(fileInfo.LastWriteTimeUtc).TotalMinutes < MINUTES_TO_WRITE_DOWNLOAD)
            {
                _logger.LogInformation($"The file {fileName} is downloading.");
                return false;
            }

            if (!allowDuplicates &&
                (File.Exists($@"{downloadPath}\{fileName}") ||
                File.Exists($@"{downloadPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSED}") ||
                File.Exists($@"{retryPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                File.Exists($@"{retryPath}\{name}{Constants.EXTENSION_RETRY}")))
            {
                _logger.LogError($"The file {fileName} is already processed.");

                if (AddRepeatedFile(userConfiguration, file))
                {
                    new FileRepeatedError(_configuration).SendErrorEmail(fileName, userConfiguration.Alerts);
                }

                return false;
            }

            return true;
        }

        private bool AddRepeatedFile(IUserConfiguration userConfiguration, string fileName)
        {
            lock (_lockRepeatedFiles)
            {
                if (!_repeatedFiles.ContainsKey(userConfiguration.Name))
                {
                    _repeatedFiles.Add(userConfiguration.Name, new List<RepeatedFile>());
                }

                if (!_repeatedFiles[userConfiguration.Name].Any(x => x.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _repeatedFiles[userConfiguration.Name].Add(new RepeatedFile()
                    {
                        Name = fileName,
                        RepeatedTime = DateTime.UtcNow
                    });

                    return true;
                }
            }

            return false;
        }

        private void RemoveRepeatedFiles(IUserConfiguration userConfiguration)
        {
            lock (_lockRepeatedFiles)
            {
                if (_repeatedFiles.ContainsKey(userConfiguration.Name))
                {
                    IEnumerable<RepeatedFile> filesToDelete = _repeatedFiles[userConfiguration.Name].Where(x => DateTime.UtcNow.Subtract(x.RepeatedTime).TotalMinutes > MINUTES_TO_DELETE_REPEATED);

                    foreach (RepeatedFile file in filesToDelete)
                    {
                        RemoveFileFromFtp(file.Name, userConfiguration);
                    }

                    _repeatedFiles[userConfiguration.Name].RemoveAll(x => DateTime.UtcNow.Subtract(x.RepeatedTime).TotalMinutes > MINUTES_TO_DELETE_REPEATED);

                    if (_repeatedFiles[userConfiguration.Name].Count == 0)
                    {
                        _repeatedFiles.Remove(userConfiguration.Name);
                    }
                }
            }
        }

        private void RemoveFileFromFtp(string file, IUserConfiguration user)
        {
            if (user.HasDeleteFtp)
            {
                _logger.LogDebug($"Remove file {file} from FTP ");

                IFtpHelper ftpHelper = user.Ftp.GetFtpHelper(_logger);

                ftpHelper.DeleteFile(file);
            }
        }
    }
}
