using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Classes;

namespace Doppler.BulkSender.Configuration
{
    public class SftpConfiguration : IFtpConfiguration
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public bool HasSSL { get; set; }

        public IFtpHelper GetFtpHelper(ILogger log)
        {
            var ftpHelper = new SftpHelper(log, this.Host, this.Port, this.Username, this.Password);

            return ftpHelper;
        }
    }
}
