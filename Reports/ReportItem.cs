using System.Collections.Generic;
using System.Linq;

namespace Doppler.BulkSender.Reports
{
    public class ReportItem
    {
        private string[] _values;

        public string ResultId { get; set; }

        public ReportItem(int count)
        {
            _values = new string[count];
        }

        public void AddValue(string value)
        {
            //_values.Add(value);
        }

        public List<string> GetValues()
        {
            return _values.ToList();
        }

        public void AddValue(string value, int position)
        {
            if (position == -1)
            {
                return;
            }

            if (position >= 0 && position <= _values.Length)
            {
                _values[position] = value;
            }
        }
    }
}
