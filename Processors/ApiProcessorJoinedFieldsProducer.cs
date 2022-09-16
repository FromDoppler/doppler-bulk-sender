using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorJoinedFieldsProducer : ApiProcessorProducer
    {
        public ApiProcessorJoinedFieldsProducer(IAppConfiguration configuration) : base(configuration)
        {
        }

        protected override void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            base.FillRecipientCustoms(recipient, data, headerList, fields);

            var field = fields.OfType<JoinedFieldConfiguration>().FirstOrDefault();

            string joinedField = data[field.Position];

            string[] joinedFieldArray = joinedField.Split(field.FieldSeparator);

            var list = new List<Dictionary<string, object>>();

            foreach (string pair in joinedFieldArray)
            {
                var auxDictionary = new Dictionary<string, object>();

                string[] pairArray = pair.Split(field.KeyValueSeparator);

                if (pairArray.Length > 1)
                {
                    string key = pairArray[0];
                    string value = pairArray[1];

                    auxDictionary.Add("header", key);
                    auxDictionary.Add("value", value);

                    list.Add(auxDictionary);
                }
            }

            recipient.Fields.Add("list", list);
        }
    }
}
