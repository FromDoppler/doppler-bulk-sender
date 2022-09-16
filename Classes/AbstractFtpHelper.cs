using Doppler.BulkSender.Classes;

namespace Doppler.BulkSender.Classes
{
    public abstract class AbstractFtpHelper : IFtpHelper
    {
        protected string _ftpHost;
        protected string _ftpUser;
        protected string _ftpPassword;
        protected int _port;
        protected ILogger _logger;

        public AbstractFtpHelper(ILogger logger, string host, int port, string user, string password)
        {
            _ftpHost = host;
            _ftpUser = user;
            _ftpPassword = password;
            _port = port;
            _logger = logger;
        }

        public abstract bool DeleteFile(string ftpFileName);

        public abstract bool DownloadFile(string ftpFileName, string localFileName);

        public abstract bool DownloadFileWithResume(string ftpFileName, string localFileName);

        public abstract List<string> GetFileList(string folder, IEnumerable<string> filters);

        public abstract bool UploadFile(string localFileName, string ftpFileName);

        public void UploadFileAsync(string localFileName, string ftpFileName)
        {
            Thread threadUpload = new Thread(new ThreadStart(() =>
            {
                int retryCount = 0;

                while (!UploadFile(localFileName, ftpFileName) && retryCount < 3)
                {
                    retryCount++;
                }

            }));

            threadUpload.Start();
        }
    }
}
