using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Configuration
{
    public class ResultConfiguration : IResultConfiguration
    {
        public string Folder { get; set; }
        public IReportName FileName { get; set; }
        public int MaxDescriptionLength { get; set; }

        public string SaveAndGetName(string fileName, string resultsFolder)
        {
            string resultsFileName = FileName.GetReportName(fileName);
            string resultsFileNamePath = $@"{resultsFolder}\{resultsFileName}";

            return resultsFileNamePath;
        }
    }
}
