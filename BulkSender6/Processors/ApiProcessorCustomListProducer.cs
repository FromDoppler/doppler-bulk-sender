using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorCustomListProducer : ApiProcessorProducer
    {
        public ApiProcessorCustomListProducer(IAppConfiguration configuration) : base(configuration)
        {
        }

        protected override void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            base.FillRecipientCustoms(recipient, data, headerList, fields);

            IEnumerable<FieldConfiguration> listFields = fields.Where(x => x.IsForList);

            var list = new List<Dictionary<string, object>>();

            foreach (FieldConfiguration field in listFields)
            {
                if (field.Position >= data.Length)
                {
                    continue;
                }

                string value = data[field.Position];

                if (!string.IsNullOrEmpty(value) && value.Length > 4)
                {
                    value = $"XXXX-{value.Substring(value.Length - 4)}";

                    var auxDictionary = new Dictionary<string, object>();

                    auxDictionary.Add("value", value);

                    list.Add(auxDictionary);
                }
            }

            recipient.Fields.Add("list", list);
        }
    }
}
