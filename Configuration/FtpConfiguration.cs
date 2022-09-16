using Doppler.BulkSender.Classes;

namespace Doppler.BulkSender.Configuration
{
    public class FtpConfiguration : IFtpConfiguration
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public bool HasSSL { get; set; }

        public IFtpHelper GetFtpHelper(ILogger log)
        {
            var ftpHelper = new FTPHelper(log, Host, Port, Username, Password, HasSSL);

            return ftpHelper;
        }
    }
}
