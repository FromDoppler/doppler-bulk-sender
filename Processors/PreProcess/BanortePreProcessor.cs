using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Doppler.BulkSender.Processors.PreProcess
{
    public class BanortePreProcessor : BasicPreProcessor
    {
        public BanortePreProcessor(ILogger logger, IAppConfiguration configuration) : base(logger, configuration) { }

        protected override void DownloadAttachments(string fileName, IUserConfiguration userConfiguration)
        {
            ITemplateConfiguration templateConfiguration = userConfiguration.GetTemplateConfiguration(fileName);

            if (templateConfiguration == null || !templateConfiguration.Fields.Any(x => x.IsAttachment))
            {
                return;
            }

            List<int> indexes = templateConfiguration.Fields.Where(x => x.IsAttachment).Select(x => x.Position).ToList();

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fileStream))
            {
                if (templateConfiguration.HasHeaders)
                {
                    reader.ReadLine();
                }

                string line;
                string[] fields;

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    fields = line.Split(templateConfiguration.FieldSeparator);

                    if (fields.Length >= 4)
                    {
                        string customAttachmentFile = $@"{fields[0]}-{fields[1]}-{fields[2]}-{fields[3]}.pdf";

                        GetAttachmentFile(customAttachmentFile, fileName, userConfiguration);
                    }

                    foreach (int index in indexes)
                    {
                        if (index < fields.Length)
                        {
                            string attachmentFile = fields[index];

                            GetAttachmentFile(attachmentFile, fileName, userConfiguration);
                        }
                    }
                }
            }
        }
    }
}
