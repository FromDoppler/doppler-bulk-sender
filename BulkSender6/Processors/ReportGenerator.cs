using Newtonsoft.Json;
using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Doppler.BulkSender.Processors
{
    public class ReportGenerator : BaseWorker
    {
        public ReportGenerator(ILogger logger, IOptions<AppConfiguration> configuration)
            : base(logger, configuration)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CheckConfigChanges();

                    CreateReportsFile();

                    string[] files = Directory.GetFiles($"{_configuration.ReportsFolder}", "*.json");

                    foreach (string file in files)
                    {
                        string json = File.ReadAllText(file);

                        List<ReportExecution> reports = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                        bool hasChanges = false;

                        foreach (ReportExecution reportExecution in reports.Where(x => !x.Processed && x.RunDate < DateTime.UtcNow))
                        {
                            try
                            {
                                IUserConfiguration user = _users.Where(x => x.Name == reportExecution.UserName).FirstOrDefault();

                                ReportTypeConfiguration reportType = user.Reports.ReportsList.Where(x => x.ReportId == reportExecution.ReportId).FirstOrDefault();

                                ReportProcessor reportProcessor = reportType.GetReportProcessor(_configuration, _logger);

                                reportProcessor.Process(user, reportExecution);

                                hasChanges = true;
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"Error to generate report:{reportExecution.ReportId} for user:{reportExecution.UserName} -- {e}");
                            }
                        }

                        if (hasChanges)
                        {
                            json = JsonConvert.SerializeObject(reports);
                            using (var streamWriter = new StreamWriter(file, false))
                            {
                                streamWriter.Write(json);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"General error on Generate report -- {e}");
                }

                await Task.Delay(_configuration.ReportsInterval, stoppingToken);
            }
        }

        private void CreateReportsFile()
        {
            string reportFileName = $@"{_configuration.ReportsFolder}\reports.{DateTime.UtcNow.ToString("yyyyMMdd")}.json";

            if (File.Exists(reportFileName))
            {
                return;
            }

            string[] files = Directory.GetFiles($"{_configuration.ReportsFolder}", "*.json");

            List<ReportExecution> allReports = new List<ReportExecution>();

            foreach (string file in files)
            {
                string json = File.ReadAllText(file);

                List<ReportExecution> executions = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                allReports.AddRange(executions);
            }

            List<IUserConfiguration> reportUsers = _users.Where(x => x.Reports != null).ToList();
            List<ReportExecution> requests = new List<ReportExecution>();

            foreach (IUserConfiguration user in reportUsers)
            {
                foreach (ReportTypeConfiguration reportType in user.Reports.ReportsList)
                {
                    var lastExecution = allReports
                        .Where(x => x.UserName == user.Name && x.ReportId == reportType.ReportId)
                        .OrderByDescending(x => x.NextRun)
                        .FirstOrDefault();

                    List<ReportExecution> executionLists = reportType.GetReportExecution(user, lastExecution);
                    requests.AddRange(executionLists);
                }
            }

            if (requests.Count > 0)
            {
                string reports = JsonConvert.SerializeObject(requests);
                using (var streamWriter = new StreamWriter(reportFileName, false))
                {
                    streamWriter.Write(reports);
                }
            }
        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    CheckConfigChanges();

                    CreateReportsFile();

                    string[] files = Directory.GetFiles($"{_configuration.ReportsFolder}", "*.json");

                    foreach (string file in files)
                    {
                        string json = File.ReadAllText(file);

                        List<ReportExecution> reports = JsonConvert.DeserializeObject<List<ReportExecution>>(json);

                        bool hasChanges = false;

                        foreach (ReportExecution reportExecution in reports.Where(x => !x.Processed && x.RunDate < DateTime.UtcNow))
                        {
                            try
                            {
                                IUserConfiguration user = _users.Where(x => x.Name == reportExecution.UserName).FirstOrDefault();

                                ReportTypeConfiguration reportType = user.Reports.ReportsList.Where(x => x.ReportId == reportExecution.ReportId).FirstOrDefault();

                                ReportProcessor reportProcessor = reportType.GetReportProcessor(_configuration, _logger);

                                reportProcessor.Process(user, reportExecution);

                                hasChanges = true;
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"Error to generate report:{reportExecution.ReportId} for user:{reportExecution.UserName} -- {e}");
                            }
                        }

                        if (hasChanges)
                        {
                            json = JsonConvert.SerializeObject(reports);
                            using (var streamWriter = new StreamWriter(file, false))
                            {
                                streamWriter.Write(json);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"General error on Generate report -- {e}");
                }

                Thread.Sleep(_configuration.ReportsInterval);
            }
        }
    }
}
