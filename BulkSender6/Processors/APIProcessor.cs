using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Queues;

namespace Doppler.BulkSender.Processors
{
    public class APIProcessor : Processor
    {
        public APIProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration) { }

        protected override string GetBody(string file, IUserConfiguration user, int processedCount, int errorsCount)
        {
            string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\FinishProcess.es.html");

            return body.Replace("{{filename}}", Path.GetFileNameWithoutExtension(file))
                .Replace("{{time}}", user.GetUserDateTime().DateTime.ToString())
                .Replace("{{processed}}", processedCount.ToString())
                .Replace("{{errors}}", errorsCount.ToString());
        }

        protected override List<string> GetAttachments(string file, string usarName)
        {
            return new List<string>();
        }

        protected override IQueueProducer GetProducer()
        {
            IQueueProducer producer = new ApiProcessorProducer(_configuration);
            producer.ErrorEvent += Processor_ErrorEvent;

            return producer;
        }

        protected override List<IQueueConsumer> GetConsumers(int count)
        {
            var consumers = new List<IQueueConsumer>();

            for (int i = 0; i < count; i++)
            {
                IQueueConsumer consumer = new ApiProcessorConsumer(_configuration, _logger);
                consumer.ErrorEvent += Processor_ErrorEvent;
                consumer.ResultEvent += Processor_ResultEvent;
                consumers.Add(consumer);
            }

            return consumers;
        }
    }
}
