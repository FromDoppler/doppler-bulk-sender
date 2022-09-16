namespace Doppler.BulkSender.Processors.Acknowledgement
{
    public interface IAckProcessor
    {
        void ProcessAckFile(string fileName);
    }
}
