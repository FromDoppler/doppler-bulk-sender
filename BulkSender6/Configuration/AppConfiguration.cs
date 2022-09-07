namespace Doppler.BulkSender.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        public string LocalDownloadFolder { get; set; }

        public string SmtpHost { get; set; }

        public int SmtpPort { get; set; }

        public string BaseUrl { get; set; }

        public string TemplateUrl { get; set; }

        public string AccountUrl { get; set; }

        public int FtpListInterval { get; set; }

        public int MaxNumberOfThreads { get; set; }

        public int BulkEmailCount { get; set; }

        public int ReportsInterval { get; set; }

        public string AdminUser { get; set; }

        public string AdminPass { get; set; }

        public int CleanInterval { get; set; }

        public int CleanDays { get; set; }

        public int CleanAttachmentsDays { get; set; }

        public int LocalFilesInterval { get; set; }

        public string ReportsFolder { get; set; }

        public int PreProcessorInterval { get; set; }

        public string UserFiles { get; set; }

        public string PublicUserFiles { get; set; }

        public int DeliveryRetryCount { get; set; }

        public int DeliveryRetryInterval { get; set; }

        public int DeliveryFailCount { get; set; }

        public int StatusProcessorInterval { get; set; }
    }
}
