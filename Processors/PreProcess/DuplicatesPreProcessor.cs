using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public class DuplicatesPreProcessor : PreProcessor
    {
        public DuplicatesPreProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration) { }

        public override void ProcessFile(string fileName, IUserConfiguration userConfiguration)
        {
            var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
            string processedFolder = filePathHelper.GetProcessedFilesFolder();
            string downloadFolder = filePathHelper.GetDownloadsFolder();

            var directory = new DirectoryInfo(processedFolder);

            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            List<int> indexes = templateConfiguration.Fields.Where(x => x.IsKey).Select(x => x.Position).ToList();

            string name = Path.GetFileNameWithoutExtension(fileName);

            IEnumerable<string> files = directory.GetFiles()
                .Where(x => x.Name.Contains(name)
                && x.FullName != fileName)
                .Select(x => x.FullName);

            var processedKeys = new HashSet<string>();

            foreach (string file in files)
            {
                using (var streamReader = new StreamReader(file))
                {
                    if (templateConfiguration.HasHeaders)
                    {
                        streamReader.ReadLine();
                    }

                    while (!streamReader.EndOfStream)
                    {
                        string line = streamReader.ReadLine();

                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        string[] lineArray = line.Split(templateConfiguration.FieldSeparator);

                        string key = GetHashKey(lineArray, indexes);

                        if (!processedKeys.Contains(key))
                        {
                            processedKeys.Add(key);
                        }
                    }
                }
            }

            var stringBuilder = new StringBuilder();

            using (var streamReader = new StreamReader(fileName))
            {
                if (templateConfiguration.HasHeaders)
                {
                    streamReader.ReadLine();
                }

                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] lineArray = line.Split(templateConfiguration.FieldSeparator);

                    string hashKey = GetHashKey(lineArray, indexes);

                    if (!processedKeys.Contains(hashKey))
                    {
                        stringBuilder.AppendLine(line);

                        processedKeys.Add(hashKey);
                    }
                }
            }

            int count = 1;
            string auxName = $@"{name}_{count.ToString("000")}";

            while (directory.GetFiles().ToList().Any(x => x.Name.Contains(auxName)))
            {
                count++;
                auxName = $@"{name}_{count.ToString("000")}";
            }

            string newFileName = $@"{downloadFolder}\{name}_{count.ToString("000")}{Constants.EXTENSION_PROCESSING}";

            using (var streamWriter = new StreamWriter(newFileName))
            {
                streamWriter.Write(stringBuilder);
            }

            File.Delete(fileName);
        }

        private string GetHashKey(string[] lineArray, List<int> indexes)
        {
            var values = new List<string>();

            foreach (int index in indexes)
            {
                values.Add(lineArray[index]);
            }

            return string.Join("|", values);
        }
    }
}
