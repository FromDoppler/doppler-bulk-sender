using Doppler.BulkSender.Classes;
using System;

namespace Doppler.BulkSender.Classes
{
    public class ProcessResult
    {
        public int LineNumber { get; set; }
        public string ResourceId { get; set; }
        public string DeliveryLink { get; set; }
        public string Message { get; set; }
        public DateTime EnqueueTime { get; set; }
        public DateTime DequeueTime { get; set; }
        public DateTime DeliveryTime { get; set; }

        public string GetResultLine(char separator)
        {
            return $"{Constants.PROCESS_RESULT_OK}{separator}{Constants.DELIVERY_OK}{separator}{ResourceId}{separator}{DeliveryLink}";
        }
    }
}
