using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Classes.Enums;
using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Configuration.Alerts;
using System.Net;
using System.Net.Mail;

namespace Doppler.BulkSender.Reports
{
    public abstract class ReportProcessor
    {
        protected readonly ILogger _logger;
        protected readonly IAppConfiguration _configuration;
        protected readonly ReportTypeConfiguration _reportTypeConfiguration;

        public ReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration)
        {
            _logger = logger;
            _configuration = configuration;
            _reportTypeConfiguration = reportTypeConfiguration;
        }

        /// <summary>
        /// Retorna la lista de archivos para generarle los reportes necesarios.
        /// </summary>
        /// <param name="user">Configuracion del usuario.</param>
        /// <param name="reportExecution">Datos de la ejecucion.</param>
        /// <returns></returns>
        protected abstract List<string> GetFilesToProcess(IUserConfiguration user, ReportExecution reportExecution);

        /// <summary>
        /// Procesa los arhivos generando el reporte correspondiente.
        /// </summary>
        /// <param name="files">Lista de archivos para generar reporte.</param>
        /// <param name="user">Confuracion del usuario.</param>
        /// <param name="reportExecution">Datos de la ejecucion.</param>
        protected abstract List<string> ProcessFilesForReports(List<string> files, IUserConfiguration user, ReportExecution reportExecution);

        protected void UploadFileToFtp(string fileName, string ftpFolder, IFtpHelper ftpHelper)
        {
            if (File.Exists(fileName) && !string.IsNullOrEmpty(ftpFolder))
            {
                string ftpFileName = $@"{ftpFolder}/{Path.GetFileName(fileName)}";

                _logger.LogDebug($"Upload file {ftpFileName} to ftp.");

                ftpHelper.UploadFileAsync(fileName, ftpFileName);
            }
        }

        public void Process(IUserConfiguration user, ReportExecution reportExecution)
        {
            List<string> files = GetFilesToProcess(user, reportExecution);

            List<string> reports = ProcessFilesForReports(files, user, reportExecution);

            reportExecution.Processed = true;
            reportExecution.ProcessedDate = DateTime.UtcNow;

            SendReportAlert(user, reports);
        }

        protected virtual void SendReportAlert(IUserConfiguration user, List<string> files)
        {
            if (user.Alerts == null || user.Alerts.GetReportAlert() == null || files == null || files.Count == 0)
            {
                return;
            }

            ReportAlertTypeConfiguration reportAlert = user.Alerts.GetReportAlert();

            SendSmtpEmail(user.Alerts.Emails, reportAlert.Subject, reportAlert.Message, files);
        }

        protected void SendSmtpEmail(List<string> toList, string subject, string body, List<string> attachments)
        {
            var smtpClient = new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort);
            smtpClient.Credentials = new NetworkCredential(_configuration.AdminUser, _configuration.AdminPass);

            var mailMessage = new MailMessage()
            {
                Body = body,
                IsBodyHtml = true,
                Subject = subject,
                From = new MailAddress("support@dopplerrelay.com", "Doppler Relay Support")
            };

            foreach (string to in toList)
            {
                mailMessage.To.Add(to);
            }

            foreach (string file in attachments)
            {
                var attachment = new Attachment(file)
                {
                    Name = Path.GetFileName(file)
                };

                mailMessage.Attachments.Add(attachment);
            }
            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error trying to send smtp email -- {e}");
            }
        }

        protected List<string> FilterFilesByTemplate(List<string> files, IUserConfiguration user)
        {
            var filteredFiles = new List<string>();
            foreach (string file in files)
            {
                ITemplateConfiguration templateConfiguration = ((UserApiConfiguration)user).GetTemplateConfiguration(file);

                if (templateConfiguration != null && _reportTypeConfiguration.Templates.Contains(templateConfiguration.TemplateName))
                {
                    filteredFiles.Add(file);
                }
            }

            return filteredFiles;
        }

        protected virtual void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT)
        {
            List<string> guids = items.Select(it => it.ResultId).Distinct().ToList();

            var sqlHelper = new SqlHelper();

            try
            {
                int i = 0;
                while (i < guids.Count)
                {
                    // TODO use skip take from linq.
                    var aux = new List<string>();
                    for (int count = 0; i < guids.Count && count < 1000; count++)
                    {
                        aux.Add(guids[i]);
                        i++;
                    }

                    List<DBStatusDto> dbReportItemList = sqlHelper.GetResultsByDeliveryList(userId, aux);
                    foreach (DBStatusDto dbReportItem in dbReportItemList)
                    {
                        ReportItem item = items.FirstOrDefault(x => x.ResultId == dbReportItem.MessageGuid);
                        if (item != null)
                        {
                            MapDBStatusDtoToReportItem(dbReportItem, item, reportGMT, dateFormat);
                        }
                    }

                    aux.Clear();
                }

                sqlHelper.CloseConnection();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on get data from DB {e}");
                throw;
            }
        }

        // TODO: internationalize messages with user configuration.
        private void GetStatusAndDescription(DBStatusDto item, out string status, out string description)
        {
            description = string.Empty;
            status = string.Empty;

            switch (item.Status)
            {
                case (int)DeliveryStatus.Queued:
                    status = "Encolado";
                    description = "Encolado";
                    break;

                case (int)DeliveryStatus.Sent:
                    status = "Enviado";
                    if (item.OpenEventsCount > 0)
                    {
                        description = "Abierto";
                    }
                    else
                    {
                        description = "No abierto";
                    }
                    break;
                case (int)DeliveryStatus.Rejected:
                    status = "Mail rechazado";
                    break;
                case (int)DeliveryStatus.Invalid:
                    status = "Mail inválido";
                    break;
                case (int)DeliveryStatus.Retrying:
                    status = "Reintentando";
                    description = "Reintentando";
                    break;
                case (int)DeliveryStatus.Dropped:
                    status = "Descartado";
                    description = "Incluido en blacklist";
                    break;
            }

            if (item.Status == (int)DeliveryStatus.Rejected || item.Status == (int)DeliveryStatus.Invalid)
            {
                description = item.IsHard ? "Rebote Hard" : "Rebote soft";

                switch (item.MailStatus)
                {
                    case (int)MailStatus.Invalid:
                        description += " - Inválido";
                        break;
                    case (int)MailStatus.RecipientRejected:
                        description += " - Destinatario rechazado";
                        break;
                    case (int)MailStatus.TimeOut:
                        description += " - Time out";
                        break;
                    case (int)MailStatus.TransactionError:
                        description += " - Error de transacción";
                        break;
                    case (int)MailStatus.ServerRejected:
                        description += " - Servidor rechazado";
                        break;
                    case (int)MailStatus.MailRejected:
                        description += " - Mail rechazado";
                        break;
                    case (int)MailStatus.MXNotFound:
                        description += " - MX no encontrado";
                        break;
                    case (int)MailStatus.InvalidEmail:
                        description += " - Mail inválido";
                        break;
                }
            }
        }

        protected List<string> GetHeadersList(List<ReportFieldConfiguration> reportHeaders, List<string> fileHeaders = null)
        {
            var headersList = new List<string>();

            foreach (ReportFieldConfiguration header in reportHeaders)
            {
                if (!headersList.Contains(header.HeaderName) && !header.HeaderName.Equals("*"))
                {
                    headersList.Add(header.HeaderName);
                }
            }

            if (reportHeaders.Exists(x => x.HeaderName.Equals("*")) && fileHeaders != null)
            {
                string header;
                for (int i = 0; i < fileHeaders.Count; i++)
                {
                    header = fileHeaders[i];
                    if (header != Constants.HEADER_PROCESS_RESULT
                        && header != Constants.HEADER_MESSAGE_ID
                        && header != Constants.HEADER_DELIVERY_RESULT
                        && header != Constants.HEADER_DELIVERY_LINK
                        && !reportHeaders.Exists(x => x.NameInFile == header))
                    {
                        if (!headersList.Contains(header))
                        {
                            headersList.Add(header);
                        }
                    }
                }
            }

            return headersList;
        }

        protected List<ReportFieldConfiguration> GetHeadersIndexes(List<ReportFieldConfiguration> configurationHeaders, List<string> fileHeaders, out int processedIndex, out int resultIndex)
        {
            var reportHeaders = new List<ReportFieldConfiguration>();

            foreach (ReportFieldConfiguration header in configurationHeaders)
            {
                var reportFieldConfiguration = new ReportFieldConfiguration()
                {
                    HeaderName = header.HeaderName,
                    Position = header.Position
                };

                if (!string.IsNullOrEmpty(header.NameInDB))
                {
                    reportFieldConfiguration.NameInDB = header.NameInDB;
                }
                else if (!string.IsNullOrEmpty(header.NameInFile))
                {
                    int index = fileHeaders.FindIndex(x => x.Equals(header.NameInFile, StringComparison.OrdinalIgnoreCase));
                    if (index != -1 && !reportHeaders.Exists(x => x.HeaderName == header.HeaderName))
                    {
                        reportFieldConfiguration.NameInFile = header.NameInFile;
                        reportFieldConfiguration.PositionInFile = index;
                    }
                }

                reportHeaders.Add(reportFieldConfiguration);
            }

            if (configurationHeaders.Exists(x => x.HeaderName.Equals("*")))
            {
                string header;
                int index;
                for (int i = 0; i < fileHeaders.Count; i++)
                {
                    header = fileHeaders[i];
                    index = i;

                    if (header != Constants.HEADER_PROCESS_RESULT
                        && header != Constants.HEADER_MESSAGE_ID
                        && header != Constants.HEADER_DELIVERY_RESULT
                        && header != Constants.HEADER_DELIVERY_LINK
                        && !reportHeaders.Exists(x => x.NameInFile == header))
                    {
                        while (reportHeaders.Exists(x => x.Position == index))
                        {
                            index++;
                        }

                        reportHeaders.Add(new ReportFieldConfiguration()
                        {
                            HeaderName = header,
                            NameInFile = header,
                            PositionInFile = i,
                            Position = index
                        });
                    }
                }
            }

            processedIndex = fileHeaders.IndexOf(Constants.HEADER_PROCESS_RESULT);
            resultIndex = fileHeaders.IndexOf(Constants.HEADER_MESSAGE_ID);

            return reportHeaders;
        }

        protected void MapDBStatusDtoToReportItem(DBStatusDto dbStatusDto, ReportItem reportItem, int reportGMT = 0, string dateFormat = "")
        {
            string status;
            string description;
            GetStatusAndDescription(dbStatusDto, out status, out description);

            foreach (ReportFieldConfiguration reportField in _reportTypeConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInDB)))
            {
                switch (reportField.NameInDB)
                {
                    case "CreatedAt":
                        reportItem.AddValue(dbStatusDto.CreatedAt.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "Status":
                        reportItem.AddValue(status, reportField.Position);
                        break;
                    case "Description":
                        reportItem.AddValue(description, reportField.Position);
                        break;
                    case "ClickEventsCount":
                        reportItem.AddValue(dbStatusDto.ClickEventsCount.ToString(), reportField.Position);
                        break;
                    case "OpenEventsCount":
                        reportItem.AddValue(dbStatusDto.OpenEventsCount.ToString(), reportField.Position);
                        break;
                    case "SentAt":
                        reportItem.AddValue(dbStatusDto.SentAt.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "Subject":
                        reportItem.AddValue(dbStatusDto.Subject, reportField.Position);
                        break;
                    case "FromEmail":
                        reportItem.AddValue(dbStatusDto.FromEmail, reportField.Position);
                        break;
                    case "FromName":
                        reportItem.AddValue(dbStatusDto.FromName, reportField.Position);
                        break;
                    case "Address":
                        reportItem.AddValue(dbStatusDto.Address, reportField.Position);
                        break;
                    case "OpenDate":
                        reportItem.AddValue(dbStatusDto.OpenDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "ClickDate":
                        if (dbStatusDto.ClickDate.HasValue)
                        {
                            reportItem.AddValue(dbStatusDto.ClickDate.Value.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        }
                        else
                        {
                            reportItem.AddValue(string.Empty, reportField.Position);
                        }
                        break;
                    case "BounceDate":
                        reportItem.AddValue(dbStatusDto.BounceDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        break;
                    case "LinkUrl":
                        reportItem.AddValue(dbStatusDto.LinkUrl, reportField.Position);
                        break;
                    case "Unsubscribed":
                        reportItem.AddValue(dbStatusDto.Unsubscribed.ToString(), reportField.Position);
                        break;
                    case "TemplateId":
                        reportItem.AddValue(dbStatusDto.TemplateId.ToString(), reportField.Position);
                        break;
                    case "TemplateName":
                        reportItem.AddValue(dbStatusDto.TemplateName, reportField.Position);
                        break;
                    case "MessageGuid":
                        reportItem.AddValue(dbStatusDto.MessageGuid, reportField.Position);
                        break;
                    case "DeliveryGuid":
                        reportItem.AddValue(dbStatusDto.DeliveryGuid, reportField.Position);
                        break;
                }
            }
        }

        protected void MapDBSummarizedDtoToReportItem(DBSummarizedDto dbSummarizedDto, ReportItem reportItem, int reportGMT = 0, string dateFormat = "")
        {
            foreach (ReportFieldConfiguration reportField in _reportTypeConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInDB)))
            {
                switch (reportField.NameInDB)
                {
                    case "TemplateId":
                        reportItem.AddValue(dbSummarizedDto.TemplateId.ToString(), reportField.Position);
                        break;
                    case "TemplateName":
                        reportItem.AddValue(dbSummarizedDto.TemplateName, reportField.Position);
                        break;
                    case "TemplateGuid":
                        reportItem.AddValue(dbSummarizedDto.TemplateGuid, reportField.Position);
                        break;
                    case "TemplateFromEmail":
                        reportItem.AddValue(dbSummarizedDto.TemplateFromEmail, reportField.Position);
                        break;
                    case "TemplateFromName":
                        reportItem.AddValue(dbSummarizedDto.TemplateFromName, reportField.Position);
                        break;
                    case "TemplateSubject":
                        reportItem.AddValue(dbSummarizedDto.TemplateSubject, reportField.Position);
                        break;
                    case "TotalDeliveries":
                        reportItem.AddValue(dbSummarizedDto.TotalDeliveries.ToString(), reportField.Position);
                        break;
                    case "TotalRetries":
                        reportItem.AddValue(dbSummarizedDto.TotalRetries.ToString(), reportField.Position);
                        break;
                    case "TotalOpens":
                        reportItem.AddValue(dbSummarizedDto.TotalOpens.ToString(), reportField.Position);
                        break;
                    case "TotalUniqueOpens":
                        reportItem.AddValue(dbSummarizedDto.TotalUniqueOpens.ToString(), reportField.Position);
                        break;
                    case "LastOpenDate":
                        if (dbSummarizedDto.LastOpenDate != DateTime.MinValue)
                        {
                            reportItem.AddValue(dbSummarizedDto.LastOpenDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        }
                        break;
                    case "TotalClicks":
                        reportItem.AddValue(dbSummarizedDto.TotalClicks.ToString(), reportField.Position);
                        break;
                    case "TotalUniqueClicks":
                        reportItem.AddValue(dbSummarizedDto.TotalUniqueClicks.ToString(), reportField.Position);
                        break;
                    case "LastClickDate":
                        if (dbSummarizedDto.LastClickDate != DateTime.MinValue)
                        {
                            reportItem.AddValue(dbSummarizedDto.LastClickDate.AddHours(reportGMT).ToString(dateFormat), reportField.Position);
                        }
                        break;
                    case "TotalUnsubscriptions":
                        reportItem.AddValue(dbSummarizedDto.TotalUnsubscriptions.ToString(), reportField.Position);
                        break;
                    case "TotalHardBounces":
                        reportItem.AddValue(dbSummarizedDto.TotalHardBounces.ToString(), reportField.Position);
                        break;
                    case "TotalSoftBounces":
                        reportItem.AddValue(dbSummarizedDto.TotalSoftBounces.ToString(), reportField.Position);
                        break;
                }
            }
        }
    }
}
