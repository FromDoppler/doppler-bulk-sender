namespace Doppler.BulkSender.Configuration
{
    public interface IResultConfiguration
    {
        string Folder { get; set; }
        IReportName FileName { get; set; }
        int MaxDescriptionLength { get; set; }

        string SaveAndGetName(string fileName, string resultsFolder);
    }
}
