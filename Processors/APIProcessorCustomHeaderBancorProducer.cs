using Doppler.BulkSender.Configuration;
using System.Linq;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorCustomHeaderBancorProducer : ApiProcessorProducer
    {
        public ApiProcessorCustomHeaderBancorProducer(IAppConfiguration configuration) : base(configuration)
        {
        }

        protected override string[] GetDataLine(string line, ITemplateConfiguration templateConfiguration)
        {
            string[] lineArray = line.Split(templateConfiguration.FieldSeparator);

            var charsToTrim = new char[] { '"' };

            return lineArray.Select(x => x.Trim().Trim(charsToTrim)).ToArray();
        }
    }
}
