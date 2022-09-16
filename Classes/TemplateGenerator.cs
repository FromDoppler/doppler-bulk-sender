using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Doppler.BulkSender.Classes
{
    public class TemplateGenerator
    {
        private List<TemplateItem> items;

        public TemplateGenerator()
        {
            items = new List<TemplateItem>();
        }

        public string GenerateHtml()
        {
            string body = File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\FinishProcessBanorte.es.html");

            var stringBuilder = new StringBuilder();

            foreach (TemplateItem item in items)
            {
                stringBuilder.AppendLine("<tr>");

                stringBuilder.AppendLine($"<td valign=\"middle\" height=\"30\"><span style=\"color: #2B1F00; font-weight: bold; font-family: Arial, Helvetica, sans-serif; font-size: 13px;\">{item.FileName}</span></td>");
                stringBuilder.AppendLine($"<td valign=\"middle\" height=\"30\"><span style=\"color: #2B1F00; font-weight: bold; font-family: Arial, Helvetica, sans-serif; font-size: 13px;\">{item.Date}</span></td>");
                stringBuilder.AppendLine($"<td valign=\"middle\" height=\"30\"><span style=\"color: #2B1F00; font-weight: bold; font-family: Arial, Helvetica, sans-serif; font-size: 13px;\">{item.Processed}</span></td>");
                stringBuilder.AppendLine($"<td valign=\"middle\" height=\"30\"><span style=\"color: #2B1F00; font-weight: bold; font-family: Arial, Helvetica, sans-serif; font-size: 13px;\">{item.Errors}</span></td>");

                stringBuilder.AppendLine("</tr>");
            }

            body = body.Replace("{{items}}", stringBuilder.ToString());

            return body;
        }

        public void AddItem(string fileName, string date, string processed, string errors)
        {
            items.Add(new TemplateItem()
            {
                FileName = fileName,
                Date = date,
                Processed = processed,
                Errors = errors
            });
        }

        public class TemplateItem
        {
            public string FileName { get; set; }
            public string Date { get; set; }
            public string Processed { get; set; }
            public string Errors { get; set; }
        }
    }
}
