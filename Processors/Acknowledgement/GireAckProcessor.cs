using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Processors.Acknowledgement
{
    public class GireAckProcessor : AckProcessor
    {
        public GireAckProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration)
        {
        }

        public override void ProcessAckFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception e)
                {
                    _logger.Equals($"Error on delete ack file -- {e}");
                }
            }
        }
    }
}
