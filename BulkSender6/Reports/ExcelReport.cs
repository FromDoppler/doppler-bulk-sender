using Doppler.BulkSender.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BulkSender.Reports
{
    public class ExcelReport : ReportBase
    {
        private ExcelHelper _excelHelper;
        public Dictionary<int, List<string>> CustomItems { get; set; }

        public ExcelReport()
        {
        }

        protected override void FillReport()
        {
            _reportFileName = $@"{ReportPath}\{ReportName}";
            _excelHelper = new ExcelHelper(_reportFileName, "Delivery Report");

            foreach (int key in CustomItems.Keys.OrderBy(t => t))
            {
                _excelHelper.GenerateReportRow(CustomItems[key]);
            }

            _excelHelper.GenerateReportRow(_headerList);

            foreach (ReportItem item in _items)
            {
                _excelHelper.GenerateReportRow(item.GetValues());
            }
        }

        protected override void Save()
        {
            _excelHelper.Save();
        }
    }
}
