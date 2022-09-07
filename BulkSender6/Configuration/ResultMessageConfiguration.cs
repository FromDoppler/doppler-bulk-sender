using Doppler.BulkSender.Configuration;
using System.IO;

namespace Doppler.BulkSender.Configuration
{
    public class ResultMessageConfiguration : IResultConfiguration
    {
        public string Folder { get; set; }
        public string Message { get; set; }
        public int MaxDescriptionLength { get; set; }
        public IReportName FileName { get; set; }

        public string SaveAndGetName(string fileName, string resultsFolder)
        {
            string resultsFileName = FileName.GetReportName(fileName);
            string resultsFileNamePath = $@"{resultsFolder}\{resultsFileName}";

            // TODO: Remove save file from here! Find better approach.
            try
            {
                using (var streamWriter = new StreamWriter(resultsFileNamePath))
                {
                    streamWriter.Write($"{Message}");
                }
            }
            catch { }

            return resultsFileNamePath;
        }
    }
}