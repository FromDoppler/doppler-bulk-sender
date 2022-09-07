using System.IO;
using System.Text;

namespace Doppler.BulkSender.Reports
{
    /// <summary>
    /// anda con * para todos los campos , no anda para algunos y * 
    /// </summary>
    public class CsvReport : ReportBase
    {
        protected StringBuilder _stringBuilder;
        public char Separator { get; set; }

        public CsvReport()
        {
            _stringBuilder = new StringBuilder();
        }

        protected override void FillReport()
        {
            string headerLine = string.Join(Separator.ToString(), _headerList);

            _stringBuilder.AppendLine(headerLine);

            foreach (ReportItem item in _items)
            {
                if (item.GetValues().Count == _headerList.Count)
                {
                    string itemLine = string.Join(Separator.ToString(), item.GetValues());
                    _stringBuilder.AppendLine(itemLine);
                }
            }
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
