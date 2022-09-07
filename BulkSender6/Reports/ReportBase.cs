using System;
using System.Collections.Generic;

namespace Doppler.BulkSender.Reports
{
    public abstract class ReportBase
    {
        protected List<ReportItem> _items;
        protected List<string> _headerList;
        protected string _reportFileName;
        public string ReportName { get; set; }
        public string ReportPath { get; set; }
        public int ReportGMT { get; set; }
        public int UserId { get; set; }

        public ReportBase()
        {
            _items = new List<ReportItem>();
            _headerList = new List<string>();
        }

        public void AddHeaders(List<string> headers)
        {
            _headerList.AddRange(headers);
        }

        public void AppendItems(List<ReportItem> items)
        {
            _items.AddRange(items);
        }

        public string Generate()
        {
            try
            {
                FillReport();

                Save();

                return GetReportFileName();
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected abstract void Save();

        protected abstract void FillReport();

        protected string GetReportFileName()
        {
            return _reportFileName;
        }
    }
}
