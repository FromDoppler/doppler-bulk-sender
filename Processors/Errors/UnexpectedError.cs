using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Processors.Errors
{
    public class UnexpectedError : Error
    {
        public UnexpectedError(IAppConfiguration configuration) : base(configuration)
        {
        }

        protected override string GetBody()
        {
            return "There is an unexpected error processing file {{filename}}";
        }
    }
}
