using Doppler.BulkSender.Processors.Status;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Doppler.BulkSender.Processors.Status
{
    public class StatusWorker : BaseWorker
    {
        public StatusWorker(ILogger logger, IOptions<AppConfiguration> configuration) : base(logger, configuration)
        {

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (IUserConfiguration user in _users.Where(x => x.Status != null))
                    {
                        var filePathHelper = new FilePathHelper(_configuration, user.Name);

                        StatusProcessor statusProcessor = user.GetStatusProcessor(_logger, _configuration);

                        var directoryInfo = new DirectoryInfo(filePathHelper.GetQueueFilesFolder());

                        statusProcessor.ProcessStatusFile(user, directoryInfo.GetFiles("*.status.tmp").Select(x => x.FullName).ToList());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GENERAL STATUS PROCESSOR ERROR: {ex}");
                }

                await Task.Delay(_configuration.StatusProcessorInterval, stoppingToken);
            }
        }
    }
}
