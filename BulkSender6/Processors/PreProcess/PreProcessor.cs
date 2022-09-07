using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public abstract class PreProcessor
    {
        protected readonly ILogger _logger;
        protected readonly IAppConfiguration _configuration;

        public PreProcessor(ILogger logger, IAppConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public abstract void ProcessFile(string fileName, IUserConfiguration userConfiguration);
    }
}
