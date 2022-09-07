using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Processors.Status
{
    public abstract class StatusProcessor
    {
        protected readonly ILogger _logger;
        protected readonly IAppConfiguration _configuration;
        protected const int TIME_WAIT_FILE = 1000;
        public StatusProcessor(ILogger logger, IAppConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public abstract void ProcessStatusFile(IUserConfiguration userConfiguration, List<string> statusFiles);

        protected bool IsFileInUse(string fileName)
        {
            bool locked = false;

            try
            {
                FileStream fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                fileStream.Close();
            }
            catch (IOException)
            {
                locked = true;
            }

            return locked;
        }
    }
}
