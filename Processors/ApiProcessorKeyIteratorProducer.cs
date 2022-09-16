using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Queues;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorKeyIteratorProducer : ApiProcessorProducer
    {
        private ApiRecipient _lastRecipient;

        public ApiProcessorKeyIteratorProducer(IAppConfiguration configuration) : base(configuration)
        {

        }

        protected override List<CustomHeader> GetHeaderList(string[] headersArray)
        {
            var customHeaders = new List<CustomHeader>();

            for (int i = 0; i < headersArray.Length; i++)
            {
                if (!customHeaders.Exists(x => x.HeaderName.Equals(headersArray[i])))
                {
                    customHeaders.Add(new CustomHeader()
                    {
                        HeaderName = headersArray[i],
                        Position = i
                    });
                }
            }

            return customHeaders;
        }

        protected override void EnqueueRecipient(ApiRecipient recipient, IBulkQueue queue, List<ProcessError> errors, List<ProcessResult> results)
        {
            if (_lastRecipient != null &&
                !_lastRecipient.Key.Equals(recipient.Key, StringComparison.InvariantCulture) &&
                !errors.Exists(x => x.LineNumber == _lastRecipient.LineNumber) &&
                !results.Exists(x => x.LineNumber == _lastRecipient.LineNumber))
            {
                queue.SendMessage(_lastRecipient);
            }

            _lastRecipient = recipient;
        }

        protected override void FillRecipientBasics(ApiRecipient recipient, string[] data, List<FieldConfiguration> fields, string templateId = null)
        {
            if (string.IsNullOrEmpty(recipient.Key))
            {
                base.FillRecipientBasics(recipient, data, fields, templateId);

                int position = fields.FirstOrDefault(x => x.IsKey).Position;

                string key = data[position];

                recipient.Key = key;
            }
        }

        protected override void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            foreach (CustomHeader customHeader in headerList)
            {
                if (!recipient.Fields.ContainsKey(customHeader.HeaderName) && !string.IsNullOrEmpty(data[customHeader.Position]))
                {
                    recipient.Fields.Add(customHeader.HeaderName, data[customHeader.Position]);
                }
            }

            List<Dictionary<string, object>> list;

            if (!recipient.Fields.ContainsKey("list"))
            {
                list = new List<Dictionary<string, object>>();
                recipient.Fields.Add("list", list);
            }
            else
            {
                list = recipient.Fields["list"] as List<Dictionary<string, object>>;
            }

            var item = new Dictionary<string, object>();

            List<FieldConfiguration> listFields = fields.Where(x => x.IsForList).ToList();

            foreach (FieldConfiguration fieldConfiguration in listFields)
            {
                if (!item.ContainsKey(fieldConfiguration.Name))
                {
                    item.Add(fieldConfiguration.Name, data[fieldConfiguration.Position]);
                }
            }

            list.Add(item);
        }

        protected override ApiRecipient GetRecipient(string[] recipientArray, ITemplateConfiguration templateConfiguration)
        {
            int position = templateConfiguration.Fields.FirstOrDefault(x => x.IsKey).Position;

            string key = recipientArray[position];

            if (_lastRecipient != null && !_lastRecipient.HasError && _lastRecipient.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase))
            {
                return _lastRecipient;
            }

            return new ApiRecipient();
        }

        protected override void FinishProducerProcess(IBulkQueue queue, List<ProcessError> errors, List<ProcessResult> results)
        {
            if (_lastRecipient != null &&
                !errors.Exists(x => x.LineNumber == _lastRecipient.LineNumber) &&
                !results.Exists(x => x.LineNumber == _lastRecipient.LineNumber))
            {
                queue.SendMessage(_lastRecipient);
            }
        }
    }
}
