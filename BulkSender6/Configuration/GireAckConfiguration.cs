using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.Acknowledgement;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Processors.Acknowledgement;
using System.Collections.Generic;

namespace Doppler.BulkSender.Configuration
{
    public class GireAckConfiguration : IAckConfiguration
    {
        public List<string> Extensions { get; set; }

        public IAckProcessor GetAckProcessor(ILogger logger, IAppConfiguration configuration)
        {
            return new GireAckProcessor(logger, configuration);
        }
    }
}