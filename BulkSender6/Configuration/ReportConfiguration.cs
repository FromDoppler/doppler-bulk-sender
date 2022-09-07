using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Configuration
{
    public class ReportConfiguration
    {
        public string Folder { get; set; }
        public List<ReportTypeConfiguration> ReportsList { get; set; }
    }
}
