using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.IO;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public abstract class ZipPreProcessor : PreProcessor
    {
        public ZipPreProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration)
        {
        }

        public string GetUnzipFolder(string fileName, IUserConfiguration userConfiguration)
        {
            if (!Path.GetExtension(fileName).Equals(Constants.EXTENSION_ZIP, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string name = Path.GetFileNameWithoutExtension(fileName);

            string downloadFolder = filePathHelper.GetDownloadsFolder();

            string unzipFolder = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{name}";

            Directory.CreateDirectory(unzipFolder);

            var zipHelper = new ZipHelper(_logger);
            zipHelper.UnzipAll(fileName, unzipFolder);

            File.Delete(fileName);

            return unzipFolder;
        }
    }
}
