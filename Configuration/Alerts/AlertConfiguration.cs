namespace Doppler.BulkSender.Configuration.Alerts
{
    public class AlertConfiguration
    {
        public List<string> Emails { get; set; }
        public List<IAlertTypeConfiguration> AlertList { get; set; }

        public ErrorAlertTypeConfiguration GetErrorAlert()
        {
            return AlertList.OfType<ErrorAlertTypeConfiguration>().FirstOrDefault();
        }

        public StartAlertTypeConfiguration GetStartAlert()
        {
            return AlertList.OfType<StartAlertTypeConfiguration>().FirstOrDefault();
        }

        public EndAlertTypeConfiguration GetEndAlert()
        {
            return AlertList.OfType<EndAlertTypeConfiguration>().FirstOrDefault();
        }

        public ReportAlertTypeConfiguration GetReportAlert()
        {
            return AlertList.OfType<ReportAlertTypeConfiguration>().FirstOrDefault();
        }
    }
}
