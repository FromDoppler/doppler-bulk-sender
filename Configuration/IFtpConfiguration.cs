using Doppler.BulkSender.Classes;

namespace Doppler.BulkSender.Configuration
{
    public interface IFtpConfiguration
    {
        string Host { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        int Port { get; set; }
        bool HasSSL { get; set; }

        IFtpHelper GetFtpHelper(ILogger log);
    }
}
