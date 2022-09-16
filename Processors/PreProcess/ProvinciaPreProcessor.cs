using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public class ProvinciaPreProcessor : PreProcessor
    {
        private readonly Dictionary<string, string> _hostedFiles;

        public ProvinciaPreProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration)
        {
            _hostedFiles = new Dictionary<string, string>();
        }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            if (!File.Exists(fileName) || !Path.GetExtension(fileName).Equals(Constants.EXTENSION_ZIP, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);

                string name = Path.GetFileNameWithoutExtension(fileName);

                string downloadFolder = filePathHelper.GetDownloadsFolder();

                string unzipFolder = $@"{filePathHelper.GetAttachmentsFilesFolder()}\{name}";

                //TODO: esto es solo para los errores con archivos grandes. si se soluciona habria que sacarlo porque nunca existe.
                //pongo el zip suelto y en attachments la carpeta ya descomprimida. para generar el processing
                if (!Directory.Exists(unzipFolder) || Directory.GetFiles(unzipFolder).Length == 0)
                {
                    Directory.CreateDirectory(unzipFolder);

                    var zipHelper = new ZipHelper(_logger);

                    zipHelper.UnzipAll(fileName, unzipFolder);
                }

                File.Delete(fileName);

                string baproFile = $@"{unzipFolder}\EnviosControl.txt";

                if (File.Exists(baproFile))
                {
                    string processingFile = $@"{downloadFolder}\{Path.GetFileNameWithoutExtension(fileName)}{Constants.EXTENSION_PROCESSING}";

                    var stringBuilder = new StringBuilder();

                    ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

                    using (var streamReader = new StreamReader(baproFile))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            string[] lineArray = line.Split(templateConfiguration.FieldSeparator);

                            lineArray[5] = GetHostedFilePath(fileName, lineArray[5], unzipFolder);

                            line = string.Join(templateConfiguration.FieldSeparator.ToString(), lineArray);

                            stringBuilder.AppendLine(line);
                        }
                    }

                    if (stringBuilder.Length > 0)
                    {
                        using (var streamWriter = new StreamWriter(processingFile))
                        {
                            streamWriter.Write(stringBuilder);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR PROVINCIA PRE PROCESSOR: {e}");
            }
        }

        private string GetHostedFilePath(string fileName, string imageFileName, string imageFileFolder)
        {
            string publicPath = string.Empty;

            if (string.IsNullOrEmpty(imageFileName))
            {
                imageFileName = "pieza.jpg";
            }

            if (_hostedFiles.ContainsKey(imageFileName))
            {
                publicPath = _hostedFiles[imageFileName];
            }
            else
            {
                string imageFilePath = $@"{imageFileFolder}\{imageFileName}";

                if (File.Exists(imageFilePath))
                {
                    string hostedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.{Path.GetFileNameWithoutExtension(imageFileName)}_{DateTime.Now.Ticks}{Path.GetExtension(imageFileName)}";

                    string privatePath = $@"{_configuration.UserFiles}\{hostedFileName}";

                    File.Copy(imageFilePath, privatePath);

                    publicPath = $"{_configuration.PublicUserFiles}/{hostedFileName}";

                    _hostedFiles.Add(imageFileName, publicPath);
                }
            }

            return publicPath;
        }
    }
}
