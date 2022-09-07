using System;
using System.Collections.Concurrent;

namespace Doppler.BulkSender.Queues
{
    public class MemoryBulkQueue : IBulkQueue
    {
        private ConcurrentQueue<IBulkQueueMessage> concurrentQueue;

        public MemoryBulkQueue()
        {
            concurrentQueue = new ConcurrentQueue<IBulkQueueMessage>();
        }

        public IBulkQueueMessage ReceiveMessage()
        {
            IBulkQueueMessage message = null;

            concurrentQueue.TryDequeue(out message);

            if (message != null)
            {
                message.DequeueTime = DateTime.UtcNow;
            }

            return message;
        }

        public void SendMessage(IBulkQueueMessage bulkQueueMessage)
        {
            bulkQueueMessage.EnqueueTime = DateTime.UtcNow;

            concurrentQueue.Enqueue(bulkQueueMessage);
        }

        public int GetCount()
        {
            return concurrentQueue.Count;
        }
    }
}
