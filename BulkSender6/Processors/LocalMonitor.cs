using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Microsoft.Extensions.Options;

namespace Doppler.BulkSender.Processors
{
    public class LocalMonitor : BaseWorker
    {
        private const int MINUTES_TO_CHECK = 60;
        private Dictionary<string, List<ProcessingFile>> _processingFiles;
        private object _lockProcessingFiles;

        public LocalMonitor(ILogger<LocalMonitor> logger, IOptions<AppConfiguration> configuration) : base(logger, configuration)
        {
            _processingFiles = new Dictionary<string, List<ProcessingFile>>();
            _lockProcessingFiles = new object();
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
                        ReprocessFailedFiles(user);

                        var filesToProcess = new List<string>();

                        var filePathHelper = new FilePathHelper(_configuration, user.Name);

                        string searchPattern = $"*{Constants.EXTENSION_RETRY}";

                        filesToProcess.AddRange(Directory.GetFiles(filePathHelper.GetRetriesFilesFolder(), searchPattern));

                        searchPattern = $"*{Constants.EXTENSION_PROCESSING}";

                        filesToProcess.AddRange(Directory.GetFiles(filePathHelper.GetDownloadsFolder(), searchPattern));

                        foreach (string file in filesToProcess)
                        {
                            if (!ProcessFile(file, user, filePathHelper))
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GENERAL LOCAL MONITOR ERROR: {ex}");
                }

                await Task.Delay(_configuration.LocalFilesInterval, stoppingToken);
            }
        }

        private void ReprocessFailedFiles(IUserConfiguration user)
        {
            UpdateProcessingFiles(user);

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            string searchPattern = $"*{Constants.EXTENSION_PROCESSING}";

            List<string> retryFiles = Directory.GetFiles(filePathHelper.GetRetriesFilesFolder(), searchPattern).ToList();

            foreach (string file in retryFiles)
            {
                if (!IsFileProcessing(user.Name, file))
                {
                    string newRetryFile = $@"{filePathHelper.GetRetriesFilesFolder()}\{Path.GetFileNameWithoutExtension(file)}{Constants.EXTENSION_RETRY}";

                    File.Move(file, newRetryFile);
                }
            }

            List<string> processFiles = Directory.GetFiles(filePathHelper.GetProcessedFilesFolder(), searchPattern).ToList();

            foreach (string file in processFiles)
            {
                if (!IsFileProcessing(user.Name, file))
                {
                    string newRetryFile = $@"{filePathHelper.GetRetriesFilesFolder()}\{Path.GetFileNameWithoutExtension(file)}{Constants.EXTENSION_RETRY}";

                    File.Move(file, newRetryFile);
                }
            }
        }

        private void UpdateProcessingFiles(IUserConfiguration user)
        {
            List<string> processingFiles = null;

            lock (_lockProcessingFiles)
            {
                if (_processingFiles.ContainsKey(user.Name))
                {
                    processingFiles = _processingFiles[user.Name].Where(x => DateTime.UtcNow.Subtract(x.LastUpdate).TotalMinutes > MINUTES_TO_CHECK).Select(x => x.FileName).ToList();
                }
            }

            if (processingFiles == null || processingFiles.Count == 0)
            {
                return;
            }

            var filePathHelper = new FilePathHelper(_configuration, user.Name);

            var queueDirectory = new DirectoryInfo(filePathHelper.GetQueueFilesFolder());

            foreach (string file in processingFiles)
            {
                List<FileInfo> queueFiles = queueDirectory.GetFiles($"{file}.*").ToList();

                if (queueFiles.Any())
                {
                    lock (_lockProcessingFiles)
                    {
                        _processingFiles[user.Name].FirstOrDefault(x => x.FileName == file).LastUpdate = DateTime.UtcNow;
                    }
                }
                else
                {
                    string processingFile = $@"{filePathHelper.GetProcessedFilesFolder()}\{file}{Constants.EXTENSION_PROCESSING}";
                    string retryFile = $@"{filePathHelper.GetRetriesFilesFolder()}\{file}{Constants.EXTENSION_PROCESSING}";

                    if (!File.Exists(processingFile) && !File.Exists(retryFile))
                    {
                        RemoveProcessingFile(user.Name, processingFile);
                    }
                    else
                    {
                        //TODO enviar alerta
                    }
                }
            }
        }

        private bool ProcessFile(string fileName, IUserConfiguration user, FilePathHelper filePathHelper)
        {
            int threadsUserCount = GetProcessorsCount(user.Name);

            int parallelProcessors = user.MaxParallelProcessors != 0 ? user.MaxParallelProcessors : _configuration.MaxNumberOfThreads;

            if (threadsUserCount >= parallelProcessors)
            {
                _logger.LogDebug($"There is no thread available for user {user.Name}. Is working with {threadsUserCount} threads.");
                return false;
            }

            var processor = user.GetProcessor(_logger, _configuration, fileName);

            if (processor == null)
            {
                // aca hay que borrar el archivo o moverlo porque no tiene procesador
                _logger.LogError($"Error to process file:{fileName}. Can't find processor for file.");
                return true;
            }

            string destFileName = Path.GetExtension(fileName) != Constants.EXTENSION_RETRY ?
                $@"{filePathHelper.GetProcessedFilesFolder()}\{Path.GetFileName(fileName)}" :
                $@"{filePathHelper.GetRetriesFilesFolder()}\{Path.GetFileNameWithoutExtension(fileName)}{Constants.EXTENSION_PROCESSING}";

            File.Move(fileName, destFileName);

            _logger.LogDebug($"New thread for user:{user.Name}. Thread count:{threadsUserCount + 1}");

            var threadState = new FileToProcess
            {
                FileName = destFileName,
                User = user                
            };

            AddProcessingFile(user.Name, destFileName);

            Task<FileProcessed> processorTask = Task.Factory.StartNew(() => processor.DoWork(threadState));
            processorTask.ContinueWith((x) =>
            {
                if (x.IsCompletedSuccessfully)
                {
                    _logger.LogDebug($"Finish to process ThreadId:{Thread.CurrentThread.ManagedThreadId} for user:{x.Result.Name}");

                    RemoveProcessingFile(x.Result.Name, x.Result.FileName);
                }
                
            });

            //TO DEBUG
            //processorTask.Wait();

            return true;
        }

        private void ProcessFinishedHandler(object sender, FileProcessed args)
        {
            _logger.LogDebug($"Finish to process ThreadId:{Thread.CurrentThread.ManagedThreadId} for user:{args.Name}");

            RemoveProcessingFile(args.Name, args.FileName);
        }

        private int GetProcessorsCount(string userName)
        {
            lock (_lockProcessingFiles)
            {
                if (_processingFiles.ContainsKey(userName))
                {
                    return _processingFiles[userName].Count;
                }

                return 0;
            }
        }

        private void AddProcessingFile(string userName, string fileName)
        {
            lock (_lockProcessingFiles)
            {
                if (!_processingFiles.ContainsKey(userName))
                {
                    var list = new List<ProcessingFile>();
                    _processingFiles.Add(userName, list);
                }

                var processingFile = new ProcessingFile()
                {
                    FileName = Path.GetFileNameWithoutExtension(fileName),
                    LastUpdate = DateTime.UtcNow
                };

                _processingFiles[userName].Add(processingFile);
            }
        }

        private void RemoveProcessingFile(string userName, string fileName)
        {
            lock (_processingFiles)
            {
                if (_processingFiles.ContainsKey(userName))
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName);

                    _processingFiles[userName].RemoveAll(x => x.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        private bool IsFileProcessing(string userName, string fileName)
        {
            lock (_processingFiles)
            {
                if (_processingFiles.ContainsKey(userName))
                {
                    return _processingFiles[userName].Any(x => x.FileName == Path.GetFileNameWithoutExtension(fileName));
                }

                return false;
            }
        }
    }

    public class FileToProcess
    {
        public IUserConfiguration User { get; set; }
        public string FileName { get; set; }
    }

    public class FileProcessed
    {
        public string Name { get; set; }
        public string FileName { get; set; }
    }

    public class FileStatus
    {
        public string FileName { get; set; }
        public int Total { get; set; }
        public int Processed { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Finished { get; set; }
    }
}
