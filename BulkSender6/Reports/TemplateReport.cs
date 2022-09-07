using Doppler.BulkSender.Classes;
using System.IO;
using System.Text;

namespace Doppler.BulkSender.Reports
{
    public class TemplateReport : ReportBase
    {
        protected StringBuilder _stringBuilder;
        public char Separator { get; set; }

        public TemplateReport()
        {
            _stringBuilder = new StringBuilder();
        }

        protected override void FillReport()
        {
            var templateGenerator = new TemplateGenerator();

            foreach (ReportItem item in _items)
            {
                string[] values = item.GetValues().ToArray();
                templateGenerator.AddItem(Path.GetFileNameWithoutExtension(values[0]), values[1], values[2], values[3]);
            }

            _stringBuilder.Append(templateGenerator.GenerateHtml());
        }

        protected override void Save()
        {
            _reportFileName = $@"{ReportPath}\{ReportName}";

            using (var streamWriter = new StreamWriter(_reportFileName))
            {
                streamWriter.Write(_stringBuilder.ToString());
            }
        }
    }
}
