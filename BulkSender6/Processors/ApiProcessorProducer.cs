using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Queues;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorProducer : IQueueProducer
    {
        private readonly IAppConfiguration _configuration;
        public event EventHandler<QueueErrorEventArgs> ErrorEvent;

        public ApiProcessorProducer(IAppConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void GetMessages(IUserConfiguration userConfiguration, IBulkQueue queue, List<ProcessError> errors, List<ProcessResult> results, string localFileName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(localFileName))
            {
                return;
            }

            string fileName = Path.GetFileName(localFileName);

            ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)userConfiguration).GetTemplateConfiguration(fileName);

            if (templateConfiguration == null)
            {
                throw new Exception("There is not template configuration.");
            }

            int lineNumber = 0;

            using (StreamReader reader = new StreamReader(localFileName))
            {
                string line = null;

                if (templateConfiguration.HasHeaders)
                {
                    line = reader.ReadLine();
                    lineNumber++;
                }

                string[] headersArray = GetHeaderLine(line, templateConfiguration);

                List<CustomHeader> customHeaders = GetHeaderList(headersArray);

                var filePathHelper = new FilePathHelper(_configuration, userConfiguration.Name);
                string attachmentsFolder = filePathHelper.GetAttachmentsFilesFolder(Path.GetFileNameWithoutExtension(localFileName));

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        lineNumber++;
                        continue;
                    }

                    string[] recipientArray = GetDataLine(line, templateConfiguration);

                    ApiRecipient recipient = GetRecipient(recipientArray, templateConfiguration);
                    recipient.LineNumber = lineNumber;

                    if (recipientArray.Length == headersArray.Length)
                    {
                        FillRecipientBasics(recipient, recipientArray, templateConfiguration.Fields, templateConfiguration.TemplateId);
                        FillRecipientCustoms(recipient, recipientArray, customHeaders, templateConfiguration.Fields);
                        FillRecipientAttachments(recipient, templateConfiguration, recipientArray, attachmentsFolder);

                        if (string.IsNullOrEmpty(recipient.TemplateId))
                        {
                            recipient.HasError = true;
                            recipient.ResultLine = "Has not template to send.";
                        }
                        else if (string.IsNullOrEmpty(recipient.ToEmail))
                        {
                            recipient.HasError = true;
                            recipient.ResultLine = "Has not email to send.";
                        }
                    }
                    else
                    {
                        recipient.HasError = true;
                        recipient.ResultLine = "The fields number is different to headers number.";
                    }

                    if (!recipient.HasError)
                    {
                        EnqueueRecipient(recipient, queue, errors, results);
                    }
                    else
                    {
                        var args = new QueueErrorEventArgs()
                        {
                            LineNumber = recipient.LineNumber,
                            Message = recipient.ResultLine,
                            Date = DateTime.UtcNow,
                            Type = ErrorType.PROCESS
                        };

                        ErrorEvent?.Invoke(this, args);
                    }

                    lineNumber++;
                }

                FinishProducerProcess(queue, errors, results);
            }
        }

        protected virtual void FinishProducerProcess(IBulkQueue queue, List<ProcessError> errors, List<ProcessResult> results)
        {

        }

        private string[] GetHeaderLine(string line, ITemplateConfiguration templateConfiguration)
        {
            string[] headersArray = { };

            if (templateConfiguration.HasHeaders)
            {
                headersArray = line.Split(templateConfiguration.FieldSeparator);
            }
            else
            {
                headersArray = templateConfiguration.Fields.Select(x => x.Name).ToArray();
            }

            if (templateConfiguration.Fields.Max(x => x.Position) >= headersArray.Length)
            {
                throw new Exception("There are missing headers.");
            }

            return headersArray;
        }

        protected virtual List<CustomHeader> GetHeaderList(string[] headersArray)
        {
            var headerList = new List<CustomHeader>();

            for (int i = 0; i < headersArray.Length; i++)
            {
                if (headerList.Exists(h => h.Position == i))
                {
                    continue;
                }

                var customHeader = new CustomHeader()
                {
                    Position = i,
                    HeaderName = headersArray[i]
                };
                headerList.Add(customHeader);
            }

            return headerList;
        }

        protected virtual string[] GetDataLine(string line, ITemplateConfiguration templateConfiguration)
        {
            return line.Split(templateConfiguration.FieldSeparator);
        }

        protected virtual ApiRecipient GetRecipient(string[] recipientArray, ITemplateConfiguration templateConfiguration)
        {
            return new ApiRecipient();
        }

        protected virtual void FillRecipientBasics(ApiRecipient recipient, string[] data, List<FieldConfiguration> fields, string templateId = null)
        {
            var fromEmailField = fields.Find(f => f.Name.Equals("fromEmail", StringComparison.OrdinalIgnoreCase));
            if (fromEmailField != null)
            {
                recipient.FromEmail = data[fromEmailField.Position];
            }

            var fromNameField = fields.Find(f => f.Name.Equals("fromName", StringComparison.OrdinalIgnoreCase));
            if (fromNameField != null)
            {
                recipient.FromName = data[fromNameField.Position];
            }

            var emailField = fields.Find(f => f.Name.Equals("email", StringComparison.OrdinalIgnoreCase));
            if (emailField != null)
            {
                recipient.ToEmail = data[emailField.Position];
            }

            var nameField = fields.Find(f => f.Name.Equals("name", StringComparison.OrdinalIgnoreCase));
            if (nameField != null)
            {
                recipient.ToName = data[nameField.Position];
            }

            var replyTo = fields.Find(f => f.Name.Equals("replyTo", StringComparison.OrdinalIgnoreCase));
            if (replyTo != null)
            {
                recipient.ReplyToEmail = data[replyTo.Position];
            }

            var replyToName = fields.Find(f => f.Name.Equals("replyToName", StringComparison.OrdinalIgnoreCase));
            if (replyToName != null)
            {
                recipient.ReplyToName = data[replyToName.Position];
            }

            var cc = fields.Find(f => f.Name.Equals("cc", StringComparison.OrdinalIgnoreCase));
            if (cc != null)
            {
                recipient.CCEmail = data[cc.Position];
            }

            var bcc = fields.Find(f => f.Name.Equals("bcc", StringComparison.OrdinalIgnoreCase));
            if (bcc != null)
            {
                recipient.BCCEmail = data[bcc.Position];
            }

            var templateField = fields.Find(f => f.Name.Equals("templateid", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(templateId) && templateField != null)
            {
                recipient.TemplateId = data[templateField.Position];
            }
            else
            {
                recipient.TemplateId = templateId;
            }
        }

        protected virtual void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (fields.Exists(f => f.Position == i && (f.IsBasic || f.IsAttachment)) || string.IsNullOrEmpty(data[i]))
                {
                    continue;
                }

                CustomHeader customHeader = headerList.First(h => h.Position == i);

                if (customHeader != null)
                {
                    recipient.Fields.Add(customHeader.HeaderName, data[i]);
                }
            }
        }

        protected virtual void FillRecipientAttachments(ApiRecipient recipient, ITemplateConfiguration templateConfiguration, string[] recipientArray, string attachmentsFolder)
        {
            foreach (FieldConfiguration field in templateConfiguration.Fields.Where(x => x.IsAttachment))
            {
                string attachName = recipientArray[field.Position];

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
            }
        }

        protected virtual void EnqueueRecipient(ApiRecipient recipient, IBulkQueue queue, List<ProcessError> errors, List<ProcessResult> results)
        {
            if (!errors.Exists(x => x.LineNumber == recipient.LineNumber) && !results.Exists(x => x.LineNumber == recipient.LineNumber))
            {
                queue.SendMessage(recipient);
            }
        }
    }
}
