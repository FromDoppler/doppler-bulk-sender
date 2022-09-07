using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BulkSender.Reports
{
    public class HipotecarioClicksReportProcessor : HipotecarioReportProcessor
    {
        public HipotecarioClicksReportProcessor(ILogger logger, IAppConfiguration configuration, ReportTypeConfiguration reportTypeConfiguration) : base(logger, configuration, reportTypeConfiguration)
        {
        }

        protected override void GetDataFromDB(List<ReportItem> items, string dateFormat, int userId, int reportGMT)
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

                    List<DBStatusDto> dbReportItemList = sqlHelper.GetClicksByDeliveryList(userId, aux);
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
    }
}
