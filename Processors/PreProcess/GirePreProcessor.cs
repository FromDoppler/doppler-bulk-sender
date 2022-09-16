using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public class GirePreProcessor : PreProcessor
    {
        public GirePreProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration) { }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            try
            {
                if (Path.GetExtension(fileName).Equals(Constants.EXTENSION_ZIP, StringComparison.OrdinalIgnoreCase))
                {
                    var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                    string downloadPath = filePathHelper.GetDownloadsFolder();
                    string processedPath = filePathHelper.GetProcessedFilesFolder();

                    List<string> zipEntries = new ZipHelper(_logger).UnzipFile(fileName, downloadPath);

                    try
                    {
                        File.Delete(fileName); // Delete zip file                        
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Error trying to delete zip file -- {e}");
                    }

                    foreach (string zipEntry in zipEntries)
                    {
                        string name = Path.GetFileNameWithoutExtension(zipEntry);

                        if (File.Exists($@"{downloadPath}\{name}{Constants.EXTENSION_PROCESSING}") ||
                            File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSING}"))
                        {
                            _logger.LogInformation($"The file {zipEntry} is processing.");

                            File.Delete(zipEntry);

                            continue;
                        }

                        if (File.Exists($@"{processedPath}\{name}{Constants.EXTENSION_PROCESSED}"))
                        {
                            _logger.LogError($"The file {zipEntry} is already processed.");

                            File.Delete(zipEntry);

                            continue;
                        }

                        string newFileName = zipEntry.Replace(Path.GetExtension(zipEntry), Constants.EXTENSION_PROCESSING);

                        File.Move(zipEntry, newFileName);
                    }
                }
                else
                {
                    string newFileName = fileName.Replace(Path.GetExtension(fileName), Constants.EXTENSION_PROCESSING);

                    File.Move(fileName, newFileName);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR GIRE PRE PROCESSOR: {e}");
            }
        }
    }
}
