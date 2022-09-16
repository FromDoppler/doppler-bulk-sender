using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorProvinciaProducer : ApiProcessorProducer
    {
        public ApiProcessorProvinciaProducer(IAppConfiguration configuration) : base(configuration) { }

        protected override void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            base.FillRecipientCustoms(recipient, data, headerList, fields);

            string hostedImage = data[5];

            if (!string.IsNullOrEmpty(hostedImage))
            {
                recipient.Fields.Add("hostedImage", hostedImage);
            }
            else
            {
                recipient.HasError = true;
                recipient.ResultLine = $"The file {hostedImage} to host doesn't exist.";
            }
        }

        protected override void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string attachmentsFolder)
        {
            foreach (FieldConfiguration field in templateConfiguration.Fields.Where(x => x.IsAttachment))
            {
                string[] attachments = recipientArray[field.Position].Split(';');

                foreach (string attachName in attachments)
                {
                    if (string.IsNullOrEmpty(attachName))
                    {
                        continue;
                    }

                    string localAttachement = $@"{attachmentsFolder}\{attachName}";

                    if (File.Exists(localAttachement))
                    {
                        recipient.Attachments.Add(localAttachement);
                    }
                    else
                    {
                        recipient.HasError = true;
                        recipient.ResultLine = $"The attachment file {attachName} doesn't exist.";
                    }
                }
            }
        }
    }
}