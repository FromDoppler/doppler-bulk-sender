using Experimental.System.Messaging;

namespace Doppler.BulkSender.Queues
{
    public class WindowsBulkQueue : IBulkQueue
    {
        private readonly string queueName;
        private readonly MessageQueue queue;
        public WindowsBulkQueue(string queueName)
        {
            this.queueName = queueName;
            string baseQueueName = $@".\Private$\{this.queueName}";

            if (!MessageQueue.Exists(baseQueueName))
            {
                queue = MessageQueue.Create(baseQueueName, false);
            }

            queue = new MessageQueue(baseQueueName);
        }

        public int GetCount()
        {
            return queue.GetAllMessages().Length;
        }

        public IBulkQueueMessage ReceiveMessage(int waitSeconds)
        {
            TimeSpan waitTime = TimeSpan.FromSeconds(waitSeconds);

            return (IBulkQueueMessage)queue.Receive(waitTime).Body;
        }

        public IBulkQueueMessage ReceiveMessage()
        {
            return (IBulkQueueMessage)queue.Receive().Body;
        }

        public void SendMessage(IBulkQueueMessage bulkQueueMessage)
        {
            queue.Send(bulkQueueMessage);
        }
    }
}
