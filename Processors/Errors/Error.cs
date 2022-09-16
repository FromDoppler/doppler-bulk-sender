using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Configuration.Alerts;
using System.Net;
using System.Net.Mail;

namespace Doppler.BulkSender.Processors.Errors
{
    public abstract class Error : IError
    {
        private readonly IAppConfiguration _configuration;

        public Error(IAppConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DateTime Date { get; set; }
        public string Message { get; set; }

        public virtual void Process()
        {
            throw new NotImplementedException();
        }

        public void SendErrorEmail(string file, AlertConfiguration alerts)
        {
            if (alerts != null
                && alerts.GetErrorAlert() != null
                && alerts.Emails.Count > 0)
            {
                var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

                var mailMessage = new MailMessage()
                {
                    Subject = alerts.GetErrorAlert().Subject,
                    From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support")
                };

                foreach (string email in alerts.Emails)
                {
                    mailMessage.To.Add(email);
                }

                string body = GetBody();

                if (string.IsNullOrEmpty(body))
                {
                    body = "Error processing file {{filename}}.";
                }

                mailMessage.Body = body.Replace("{{filename}}", Path.GetFileNameWithoutExtension(file));
                mailMessage.IsBodyHtml = true;

                try
                {
                    smtpClient.Send(mailMessage);
                }
                catch (Exception e)
                {
                    //TODO: log error or retry
                }
            }
        }

        protected abstract string GetBody();
    }
}
