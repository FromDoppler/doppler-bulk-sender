using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Doppler.BulkSender.Processors
{
    public class APIProcessorDuplicates : APIProcessor
    {
        public APIProcessorDuplicates(ILogger logger, IAppConfiguration configuration) : base(logger, configuration)
        {

        }

        protected override string GetBody(string file, IUserConfiguration user, int processedCount, int errorsCount)
        {
            var templateGenerator = new TemplateGenerator();
            templateGenerator.AddItem(Path.GetFileNameWithoutExtension(file), user.GetUserDateTime().DateTime.ToString(), processedCount.ToString(), errorsCount.ToString());

            return templateGenerator.GenerateHtml();
        }

        protected override List<string> GetAttachments(string file, string userName)
        {
            var attchments = new List<string>();

            string resultsFolder = new FilePathHelper(_configuration, userName).GetResultsFilesFolder();

            string name = $@"{Path.GetFileNameWithoutExtension(file)}_ERR";

            var directoryInfo = new DirectoryInfo(resultsFolder);

            return directoryInfo.GetFiles().Where(x => x.Name.Contains(name)).Select(x => x.FullName).ToList();
        }
    }
}
