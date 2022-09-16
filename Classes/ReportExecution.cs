using System;

namespace Doppler.BulkSender.Classes
{
    public class ReportExecution
    {
        public string UserName { get; set; }
        public string ReportId { get; set; }
        public DateTime LastRun { get; set; }
        public DateTime NextRun { get; set; }
        public DateTime RunDate { get; set; }
        public bool Processed { get; set; }
        public string ReportFile { get; set; }
        public string FileName { get; set; }
        public DateTime ProcessedDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}