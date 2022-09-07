using Doppler.BulkSender.Configuration.Alerts;
using Doppler.BulkSender.Processors;
using Doppler.BulkSender.Processors.Acknowledgement;
using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Processors.Status;

namespace Doppler.BulkSender.Configuration
{
    public interface IUserConfiguration
    {
        string Name { get; set; }
        int FtpInterval { get; set; }
        List<string> FileExtensions { get; set; }
        List<string> DownloadFolders { get; set; }
        string AttachmentsFolder { get; set; }
        string HostedFolder { get; set; }
        ErrorConfiguration Errors { get; set; }
        bool HasDeleteFtp { get; set; }
        IResultConfiguration Results { get; set; }
        int UserGMT { get; set; }
        int MaxParallelProcessors { get; set; }
        int DeliveryDelay { get; set; }
        int MaxThreadsNumber { get; set; }
        CredentialsConfiguration Credentials { get; set; }
        List<ITemplateConfiguration> Templates { get; set; }
        IFtpConfiguration Ftp { get; set; }
        ReportConfiguration Reports { get; set; }
        AlertConfiguration Alerts { get; set; }
        IPreProcessorConfiguration PreProcessor { get; set; }
        IStatusConfiguration Status { get; set; }
        IAckConfiguration Ack { get; set; }

        Processor GetProcessor(ILogger logger, IAppConfiguration configuration, string fileName);

        ITemplateConfiguration GetTemplateConfiguration(string fileName);

        DateTimeOffset GetUserDateTime();

        PreProcessor GetPreProcessor(ILogger logger, IAppConfiguration configuration, string fileName);

        StatusProcessor GetStatusProcessor(ILogger logger, IAppConfiguration configuration);

        IAckProcessor GetAckProcessor(ILogger logger, IAppConfiguration configuration);
    }
}