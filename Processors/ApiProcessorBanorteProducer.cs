using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.IO;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorBanorteProducer : ApiProcessorProducer
    {
        public ApiProcessorBanorteProducer(IAppConfiguration configuration) : base(configuration)
        {
        }

        protected override void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string attachmentsFolder)
        {
            string attachName = null;

            if (recipientArray.Length >= 4)
            {
                attachName = $@"{recipientArray[0]}-{recipientArray[1]}-{recipientArray[2]}-{recipientArray[3]}.pdf";
            }

            if (!string.IsNullOrEmpty(attachName))
            {
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

            base.FillRecipientAttachments(recipient, templateConfiguration, recipientArray, attachmentsFolder);
        }
    }
}
