using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Configuration;
using System;
using System.IO;

namespace Doppler.BulkSender.Processors.Errors
{
    public class FileRepeatedError : Error
    {
        public FileRepeatedError(IAppConfiguration configuration) : base(configuration)
        {

        }

        protected override string GetBody()
        {
            return File.ReadAllText($@"{AppDomain.CurrentDomain.BaseDirectory}\EmailTemplates\ErrorRepeated.es.html");
        }
    }
}
