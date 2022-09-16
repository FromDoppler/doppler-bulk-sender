using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Microsoft.Extensions.Options;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public class PreProcessWorker : BaseWorker
    {
        private readonly Dictionary<string, Task> preProcessors;

        public PreProcessWorker(ILogger<PreProcessWorker> logger, IOptions<AppConfiguration> configuration) : base(logger, configuration)
        {
            preProcessors = new Dictionary<string, Task>();
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
                        var filePathHelper = new FilePathHelper(_configuration, user.Name);

                        List<string> downloadFiles = Directory.GetFiles(filePathHelper.GetDownloadsFolder()).ToList();

                        string[] extensions = user.FileExtensions != null ? user.FileExtensions.ToArray() : new string[] { ".csv" };

                        List<string> filterFiles = downloadFiles.Where(x => extensions.Any(y => Path.GetExtension(x).Equals(y, StringComparison.OrdinalIgnoreCase))).ToList();

                        if (filterFiles.Count > 0 && !UserIsProcessing(user.Name))
                        {
                            Task preProcessorTask = Task.Factory.StartNew(() => PreProcessorWork(user, filterFiles));

                            //TO DEBUG
                            //preProcessorTask.Wait();

                            AddPreprocessorTask(preProcessorTask, user.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GENERAL PREPROCESS ERROR: {ex}");
                }

                await Task.Delay(_configuration.PreProcessorInterval, stoppingToken);
            }
        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    foreach (IUserConfiguration user in _users)
                    {
                        var filePathHelper = new FilePathHelper(_configuration, user.Name);

                        List<string> downloadFiles = Directory.GetFiles(filePathHelper.GetDownloadsFolder()).ToList();

                        string[] extensions = user.FileExtensions != null ? user.FileExtensions.ToArray() : new string[] { ".csv" };

                        List<string> filterFiles = downloadFiles.Where(x => extensions.Any(y => Path.GetExtension(x).Equals(y, StringComparison.OrdinalIgnoreCase))).ToList();

                        if (filterFiles.Count > 0 && !UserIsProcessing(user.Name))
                        {
                            Task preProcessorTask = Task.Factory.StartNew(() => PreProcessorWork(user, filterFiles));

                            //TO DEBUG
                            //preProcessorTask.Wait();

                            AddPreprocessorTask(preProcessorTask, user.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GENERAL PREPROCESS ERROR: {ex}");
                }

                Thread.Sleep(_configuration.PreProcessorInterval);
            }
        }

        private void PreProcessorWork(IUserConfiguration user, List<string> files)
        {
            foreach (string file in files)
            {
                PreProcessor preProcessor = user.GetPreProcessor(_logger, _configuration, file);

                preProcessor.ProcessFile(file, user);
            }
        }

        private void AddPreprocessorTask(Task preProcessorTask, string name)
        {
            if (preProcessors.ContainsKey(name))
            {
                preProcessors[name].Dispose();
                preProcessors.Remove(name);
            }
            preProcessors.Add(name, preProcessorTask);
        }

        private bool UserIsProcessing(string name)
        {
            if (preProcessors.ContainsKey(name))
            {
                return preProcessors[name].Status == TaskStatus.Running;
            }

            return false;
        }
    }
}
