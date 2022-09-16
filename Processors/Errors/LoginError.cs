using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Configuration;
using System;
using System.IO;

namespace Doppler.BulkSender.Processors.Errors
{
    public class LoginError : Error
    {
        public LoginError(IAppConfiguration configuration) : base(configuration)
        {

        }

        protected override string GetBody()
        {
            return File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorLogin.es.html");
        }
    }
}
