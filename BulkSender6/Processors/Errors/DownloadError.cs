using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Configuration;
using System;
using System.IO;

namespace Doppler.BulkSender.Processors.Errors
{
    public class DownloadError : Error
    {
        public DownloadError(IAppConfiguration configuration) : base(configuration)
        {

        }

        protected override string GetBody()
        {
            return File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorDownload.es.html");
        }
    }
}
