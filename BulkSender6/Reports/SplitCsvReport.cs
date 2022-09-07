using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Doppler.BulkSender.Reports
{
    /// <summary>
    /// anda con * para todos los campos , no anda para algunos y * 
    /// </summary>
    public class SplitCsvReport : ReportBase
    {
        protected StringBuilder _stringBuilder;
        public char Separator { get; set; }
        private List<string> files;
        private const int ITEMS_FOR_FILE = 100000;

        public SplitCsvReport()
        {
            _stringBuilder = new StringBuilder();
            files = new List<string>();
        }

        protected override void FillReport()
        {
            string headerLine = string.Join(Separator.ToString(), _headerList);

            _stringBuilder.AppendLine(headerLine);

            int numeric = 1;
            int count = 0;
            string reportName;

            foreach (ReportItem item in _items)
            {
                if (item.GetValues().Count == _headerList.Count)
                {
                    string itemLine = string.Join(Separator.ToString(), item.GetValues());
                    _stringBuilder.AppendLine(itemLine);
                    count++;
                }

                if (count == ITEMS_FOR_FILE)
                {
                    reportName = Save(numeric);
                    files.Add(reportName);
                    count = 0;
                    numeric++;

                    _stringBuilder.Clear();
                    _stringBuilder.AppendLine(headerLine);
                }
            }

            if (count > 0)
            {
                reportName = Save(numeric);
                files.Add(reportName);
            }
        }

        public List<string> SplitGenerate()
        {
            FillReport();

            return files;
        }

        private string Save(int numeric)
        {
            string newReportName = $"{Path.GetFileNameWithoutExtension(ReportName)}_{numeric}{Path.GetExtension(ReportName)}";

            string reportFileName = $@"{ReportPath}\{newReportName}";

            using (var streamWriter = new StreamWriter(reportFileName))
            {
                streamWriter.Write(_stringBuilder.ToString());
            }

            return reportFileName;
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
