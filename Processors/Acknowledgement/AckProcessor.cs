using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Processors.Acknowledgement
{
    public abstract class AckProcessor : IAckProcessor
    {
        protected readonly ILogger _logger;
        protected readonly IAppConfiguration _configuration;

        public AckProcessor(ILogger logger, IAppConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public abstract void ProcessAckFile(string fileName);
    }
}
