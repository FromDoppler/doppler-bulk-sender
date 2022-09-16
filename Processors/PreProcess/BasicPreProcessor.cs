using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public class BasicPreProcessor : PreProcessor
    {
        public BasicPreProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration) { }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            try
            {
                DownloadAttachments(fileName, userConfiguration);

                string newFileName = fileName.Replace(Path.GetExtension(fileName), Constants.EXTENSION_PROCESSING);

                File.Move(fileName, newFileName);
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR BASIC PRE PROCESSOR: {e}");
            }
        }

        protected virtual void DownloadAttachments(string fileName, IUserConfiguration userConfiguration)
        {
            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            if (templateConfiguration == null || !templateConfiguration.Fields.Any(x => x.IsAttachment))
            {
                return;
            }

            List<int> indexes = templateConfiguration.Fields.Where(x => x.IsAttachment).Select(x => x.Position).ToList();

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fileStream))
            {
                if (templateConfiguration.HasHeaders)
                {
                    reader.ReadLine();
                }

                string line;
                string[] fields;

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    fields = line.Split(templateConfiguration.FieldSeparator);

                    foreach (int index in indexes)
                    {
                        if (index < fields.Length)
                        {
                            string attachmentFile = fields[index];

                            GetAttachmentFile(attachmentFile, fileName, userConfiguration);
                        }
                    }
                }
            }
        }

        protected void GetAttachmentFile(string attachmentFile, string originalFile, IUserConfiguration userConfiguration)
        {
            if (string.IsNullOrEmpty(attachmentFile))
            {
                return;
            }

            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string subFolder = Path.GetFileNameWithoutExtension(originalFile);

            string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder(subFolder);

            if (!Directory.Exists(localAttachmentFolder))
            {
                Directory.CreateDirectory(localAttachmentFolder);
            }

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(originalFile);

            if (!string.IsNullOrEmpty(templateConfiguration.HostedFolder))
            {
                //TODO refactorizar en nuevos preprocessors
                GetHostedFiles(attachmentFile, originalFile, userConfiguration);
            }

            //local file 
            string localAttachmentFile = $@"{localAttachmentFolder}\{attachmentFile}";

            if (File.Exists(localAttachmentFile))
            {
                return;
            }

            //get from ftp
            string ftpAttachmentFile = $@"{templateConfiguration.AttachmentsFolder}/{attachmentFile}";

            var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);
            ftpHelper.DownloadFile(ftpAttachmentFile, localAttachmentFile);

            if (File.Exists(localAttachmentFile))
            {
                ftpHelper.DeleteFile(ftpAttachmentFile);
                return;
            }

            //get from zip file
            string zipAttachments = $@"{templateConfiguration.AttachmentsFolder}\{Path.GetFileNameWithoutExtension(originalFile)}{Constants.EXTENSION_ZIP}";
            string localZipFile = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{Path.GetFileNameWithoutExtension(originalFile)}{Constants.EXTENSION_ZIP}";

            // TODO: add retries.
            ftpHelper.DownloadFile(zipAttachments, localZipFile);

            if (File.Exists(localZipFile))
            {
                var zipHelper = new ZipHelper(_logger);
                try
                {
                    zipHelper.UnzipFile(localZipFile, localAttachmentFolder);
                }
                catch (Exception e)
                {
                    //TODO: esto hay que moverlo a un proceso de alertas con retry, tambien habria que terminar el proceso del archivo.
                    _logger.LogError($"ERROR trying to unzip file {localZipFile} -- {e}");
                }

                ftpHelper.DeleteFile(zipAttachments);
                File.Delete(localZipFile); //TODO add retries.
            }
        }

        protected void GetHostedFiles(string attachmentFile, string fileName, IUserConfiguration userConfiguration)
        {
            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

            string hostedFolder = filePathHelper.GetHostedFolder();
            string hostedFile = $@"{hostedFolder}\{attachmentFile}";

            if (!File.Exists(hostedFile))
            {
                var templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);
                string ftpHostedFile = $@"{templateConfiguration.HostedFolder}/{attachmentFile}";

                var ftpHelper = userConfiguration.Ftp.GetFtpHelper(_logger);
                ftpHelper.DownloadFile(ftpHostedFile, hostedFile);

                if (File.Exists(hostedFile))
                {
                    ftpHelper.DeleteFile(ftpHostedFile);
                }
            }

            if (File.Exists(hostedFile))
            {
                string subFolder = Path.GetFileNameWithoutExtension(fileName);
                string localAttachmentFolder = filePathHelper.GetAttachmentsFilesFolder(subFolder);
                string localFile = $@"{localAttachmentFolder}\{attachmentFile}";

                if (!File.Exists(localFile))
                {
                    File.Copy(hostedFile, localFile);
                }
            }
        }
    }
}
