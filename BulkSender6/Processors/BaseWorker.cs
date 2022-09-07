using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Doppler.BulkSender.Processors
{
    public abstract class BaseWorker : BackgroundService
    {
        protected List<IUserConfiguration> _users;
        protected readonly ILogger _logger;
        protected readonly IAppConfiguration _configuration;        
        private DateTime _lastConfigLoad;
        private const int MINUTES_TO_RELOAD = 5;

        public BaseWorker(ILogger logger, IOptions<AppConfiguration> options)
        {
            _logger = logger;
            _configuration = options.Value;
            _users = LoadUsers();
        }

        protected void CheckConfigChanges()
        {
            if (DateTime.UtcNow.Subtract(_lastConfigLoad).TotalMinutes < MINUTES_TO_RELOAD)
            {
                return;
            }

            string configFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}configs";

            var directoryInfo = new DirectoryInfo(configFilePath);

            DateTime lastWriteFile = directoryInfo.GetFiles().Max(x => x.LastWriteTimeUtc);

            DateTime lastWrite = lastWriteFile > directoryInfo.LastWriteTimeUtc ? lastWriteFile : directoryInfo.LastWriteTimeUtc;

            if (lastWrite >= _lastConfigLoad)
            {
                _logger.LogInformation($"{GetType()}. There are changes on configuration.");

                _users.Clear();

                _users = LoadUsers();
            }
        }

        private List<IUserConfiguration> LoadUsers()
        {
            var userList = new List<IUserConfiguration>();
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            string configFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}configs";

            string[] configFiles = Directory.GetFiles(configFilePath);

            foreach (string configFile in configFiles)
            {
                string fileName = Path.GetFileName(configFile);

                if (fileName.EndsWith("config.json"))
                {
                    string fileNamePath = $@"{configFilePath}\{fileName}";

                    try
                    {
                        string jsonString = File.ReadAllText(fileNamePath);

                        IUserConfiguration userConfiguration = JsonConvert.DeserializeObject<IUserConfiguration>(jsonString, jsonSerializerSettings);

                        new FilePathHelper(_configuration, userConfiguration.Name).CreateUserFolders();

                        UpdateDefaultConfigurations(userConfiguration);

                        userList.Add(userConfiguration);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Error trying to load {fileName} configuration -- {e}");
                    }
                }
            }

            _lastConfigLoad = DateTime.UtcNow;

            return userList;
        }

        /// <summary>
        /// Use defuault configuration values for
        /// - Download folders
        /// - Attachment folders
        /// - Pre Processors
        /// If there are not defined values for some template.
        /// </summary>
        /// <param name="userConfiguration"></param>
        private void UpdateDefaultConfigurations(IUserConfiguration userConfiguration)
        {
            foreach (ITemplateConfiguration templateConfiguration in userConfiguration.Templates)
            {
                if (string.IsNullOrEmpty(templateConfiguration.AttachmentsFolder))
                {
                    templateConfiguration.AttachmentsFolder = userConfiguration.AttachmentsFolder;
                }

                if (templateConfiguration.DownloadFolders == null || templateConfiguration.DownloadFolders.Count == 0)
                {
                    templateConfiguration.DownloadFolders = userConfiguration.DownloadFolders;
                }

                if (string.IsNullOrEmpty(templateConfiguration.HostedFolder))
                {
                    templateConfiguration.HostedFolder = userConfiguration.HostedFolder;
                }

                if (templateConfiguration.PreProcessor == null)
                {
                    templateConfiguration.PreProcessor = userConfiguration.PreProcessor;
                }
            }
        }
    }
}
