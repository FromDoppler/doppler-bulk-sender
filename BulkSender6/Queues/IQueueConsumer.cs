using Doppler.BulkSender.Configuration;
using System;
using System.Threading;

namespace Doppler.BulkSender.Queues
{
    public interface IQueueConsumer
    {
        event EventHandler<QueueResultEventArgs> ResultEvent;
        event EventHandler<QueueErrorEventArgs> ErrorEvent;

        void ProcessMessages(IUserConfiguration userConfiguration, IBulkQueue queue, CancellationToken cancellationToken);
    }
}
