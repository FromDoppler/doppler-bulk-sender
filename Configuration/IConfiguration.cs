namespace Doppler.BulkSender.Configuration
{
    public interface IAppConfiguration
    {
        string LocalDownloadFolder { get; }
        string SmtpHost { get; }
        int SmtpPort { get; }
        string BaseUrl { get; }
        string TemplateUrl { get; }
        string AccountUrl { get; }
        int FtpListInterval { get; }
        int MaxNumberOfThreads { get; }
        int BulkEmailCount { get; }
        int ReportsInterval { get; }
        string AdminUser { get; }
        string AdminPass { get; }
        int CleanInterval { get; }
        int CleanDays { get; }
        int CleanAttachmentsDays { get; }
        int LocalFilesInterval { get; }
        string ReportsFolder { get; }
        int PreProcessorInterval { get; }
        string UserFiles { get; }
        string PublicUserFiles { get; }
        int DeliveryRetryCount { get; }
        int DeliveryRetryInterval { get; }
        int DeliveryFailCount { get; }
        int StatusProcessorInterval { get; }
    }
}
