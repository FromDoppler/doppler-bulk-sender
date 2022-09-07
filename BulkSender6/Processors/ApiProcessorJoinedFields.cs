using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Queues;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorJoinedFields : APIProcessor
    {
        public ApiProcessorJoinedFields(ILogger logger, IAppConfiguration configuration) : base(logger, configuration) { }

        protected override IQueueProducer GetProducer()
        {
            IQueueProducer producer = new ApiProcessorJoinedFieldsProducer(_configuration);
            producer.ErrorEvent += Processor_ErrorEvent;

            return producer;
        }
    }
}
