using System.IO;
using System.Text;

namespace Doppler.BulkSender.Reports
{
    /// <summary>
    /// anda con * para todos los campos , no anda para algunos y * 
    /// </summary>
    public class CsvSplittedReport : CsvReport
    {
        private int numeric;
        private string currentFileName;

        public CsvSplittedReport() : base()
        {
            numeric = 0;
        }

        public string SplitGenerate()
        {
            FillReport();

            numeric++;

            Save();

            _items.Clear();
            _stringBuilder.Clear();

            return currentFileName;
        }

        protected override void Save()
        {
            string newReportName = $"{Path.GetFileNameWithoutExtension(ReportName)}_{numeric}{Path.GetExtension(ReportName)}";

            currentFileName = $@"{ReportPath}\{newReportName}";

            using (var streamWriter = new StreamWriter(currentFileName))
            {
                streamWriter.Write(_stringBuilder.ToString());
            }
        }
    }
}
