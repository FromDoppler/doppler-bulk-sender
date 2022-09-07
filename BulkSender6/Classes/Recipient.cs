namespace Doppler.BulkSender.Classes
{
    public class Recipient
    {
        public string ToEmail { get; set; }
        public string ToName { get; set; }
        public string CCEmail { get; set; }
        public string BCCEmail { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string ReplyToEmail { get; set; }
        public string ReplyToName { get; set; }
        public string Subject { get; set; }
        public bool HasError { get; set; }
        public string ResultLine { get; set; }
        public int LineNumber { get; set; }

        public void AddProcessedResult(string line, char separator, string message)
        {
            ResultLine = $"{line}{separator}{message}";
        }

        public void AddSentResult(char separator, string message)
        {
            ResultLine += $"{separator}{message}";
        }
    }
}
