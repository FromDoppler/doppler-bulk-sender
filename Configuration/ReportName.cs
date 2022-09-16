namespace Doppler.BulkSender.Configuration
{
    public interface IReportNamePart
    {
        string GetValue();
        void Reset();
    }

    public class FixReportNamePart : IReportNamePart
    {
        public string Value { get; set; }

        public string GetValue()
        {
            return Value;
        }

        public void Reset() { }
    }

    public class DateReportNamePart : IReportNamePart
    {
        public string GetValue()
        {
            return DateTime.UtcNow.AddHours(-3).ToString("yyyyMMdd"); // TODO: Replace for configuration GMT.
        }

        public void Reset() { }
    }

    public class DateTimeReportNamePart : IReportNamePart
    {
        public string GetValue()
        {
            return DateTime.UtcNow.AddHours(-3).ToString("yyyyMMddHHmmss");
        }

        public void Reset()
        {
        }
    }

    public class TimeReportNamePart : IReportNamePart
    {
        public string GetValue()
        {
            return DateTime.UtcNow.AddHours(-3).ToString("HHmmss");
        }

        public void Reset()
        {
        }
    }

    public class NumberReportNamePart : IReportNamePart
    {
        private int digits = 1;
        private int value = 0;

        public int Digits
        {
            get { return digits; }
            set { digits = value; }
        }

        public string GetValue()
        {
            value++;

            string auxValue = value.ToString();

            while (auxValue.Length < digits)
            {
                auxValue = string.Format("0{0}", auxValue);
            }

            return auxValue;
        }

        public void Reset()
        {
            value = 0;
        }
    }

    public class FileNameReportNamePart : IReportNamePart
    {
        private string value = "{{FILENAME}}";

        public string GetValue()
        {
            return value;
        }

        public void Reset() { }
    }

    public interface IReportName
    {
        string PartSeparator { get; set; }
        List<IReportNamePart> Parts { get; set; }

        /// <summary>
        /// Get the name for the report automatically.
        /// </summary>
        /// <param name="file">Use the original file on the report name.</param>
        /// <param name="path">User report path to avoid duplicate names.</param>
        /// <returns>Return the report name.</returns>
        string GetReportName(string file = "", string path = "");
        string Extension { get; set; }
    }

    public class ReportName : IReportName
    {
        public string Extension { get; set; }
        public string PartSeparator { get; set; }
        public List<IReportNamePart> Parts { get; set; }

        public string GetReportName(string file = "", string path = "")
        {
            string name = GetName(file);
            if (!string.IsNullOrEmpty(path))
            {
                string newName;
                while (File.Exists($@"{path}\{name}"))
                {
                    newName = GetName(file);
                    if (name == newName)
                    {
                        break;
                    }
                    name = newName;
                }
                foreach (IReportNamePart part in Parts)
                {
                    part.Reset();
                }
            }
            return name;
        }

        private string GetName(string file)
        {
            var values = new List<string>();
            foreach (IReportNamePart reportNamePart in Parts)
            {
                values.Add(reportNamePart.GetValue());
            }

            string name = $"{string.Join(PartSeparator, values)}.{Extension}";

            name = name.Replace("{{FILENAME}}", Path.GetFileNameWithoutExtension(file));

            return name;
        }
    }

    public class CustomReportName : ReportName, IReportName
    {
        public new string GetReportName(string file = "", string path = "")
        {
            string name = base.GetReportName();
            if (!string.IsNullOrEmpty(file))
            {
                List<string> parts = file.Split(PartSeparator[0]).ToList();
                parts.RemoveAll(x => Parts.OfType<FixReportNamePart>().Any(f => f.Value == x));

                for (int i = 0; i < parts.Count; i++)
                {
                    string value = $"{{v{i}}}";
                    name = name.Replace(value, parts[i]);
                }
            }
            return name;
        }
    }

    public class SimpleReportName : IReportName
    {
        public string Extension { get; set; }
        public string PartSeparator { get; set; }
        public List<IReportNamePart> Parts { get; set; }

        public string GetReportName(string file, string path = "")
        {
            return $"{Path.GetFileNameWithoutExtension(file)}.{Extension}";
        }
    }
}
