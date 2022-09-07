namespace Doppler.BulkSender.Queues
{
    public interface IBulkQueue
    {
        void SendMessage(IBulkQueueMessage bulkQueueMessage);

        IBulkQueueMessage ReceiveMessage();

        int GetCount();
    }
}
