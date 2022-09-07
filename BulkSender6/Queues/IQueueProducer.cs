using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.Errors;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Doppler.BulkSender.Queues
{
    public interface IQueueProducer
    {
        event EventHandler<QueueErrorEventArgs> ErrorEvent;

        void GetMessages(IUserConfiguration userConfiguration, IBulkQueue queue, List<ProcessError> errors, List<ProcessResult> results, string localFileName, CancellationToken cancellationToken);
    }
}
