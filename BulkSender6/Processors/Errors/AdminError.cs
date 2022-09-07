using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Configuration.Alerts;

namespace Doppler.BulkSender.Processors.Errors
{
    public class AdminError : Error
    {
        private string _user;

        public AdminError(IAppConfiguration configuration, string user) : base(configuration)
        {
            _user = user;
        }

        public override void Process()
        {
            var errorAlertTypeConfiguration = new ErrorAlertTypeConfiguration()
            {
                Subject = "BulkSender Error"
            };

            var alertConfiguration = new AlertConfiguration()
            {
                Emails = new List<string>() { "leve2support@makingsense.com" },
                AlertList = new List<IAlertTypeConfiguration>() { errorAlertTypeConfiguration }
            };

            SendErrorEmail(string.Empty, alertConfiguration);
        }

        protected override string GetBody()
        {
            return $"Problems with bulksender and ftp connection for user {_user}. Please check the application log.";
        }
    }
}
