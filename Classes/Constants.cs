namespace Doppler.BulkSender.Classes
{
    public class Constants
    {
        public const string HEADER_PROCESS_RESULT = "ProcessResult";
        public const string HEADER_DELIVERY_RESULT = "DeliveryResult";
        public const string HEADER_MESSAGE_ID = "MessageId";
        public const string HEADER_DELIVERY_LINK = "DeliveryLink";
        public const string HEADER_STATUS = "DeliveryStatus";
        public const string HEADER_OPENS = "OpensCount";
        public const string HEADER_CLICKS = "ClicksCount";
        public const string HEADER_SENT_DATE = "SentDate";

        public const string PROCESS_RESULT_OK = "Processed";
        public const string DELIVERY_OK = "Send OK";

        public const string FOLDER_ATTACHMENTS = "Attachments";
        public const string FOLDER_DOWNLOADS = "Downloads";
        public const string FOLDER_PROCESSED = "Processed";
        public const string FOLDER_QUEUES = "Queues";
        public const string FOLDER_REPORTS = "Reports";
        public const string FOLDER_RESULTS = "Results";
        public const string FOLDER_RETRIES = "Retries";
        public const string FOLDER_HOSTED = "Hosted";

        public const string EXTENSION_PROCESSING = ".processing";
        public const string EXTENSION_PROCESSED = ".processed";
        public const string EXTENSION_RETRY = ".retry";
        public const string EXTENSION_SENT = ".sent";
        public const string EXTENSION_DOWNLOADING = ".downloading";
        public const string EXTENSION_ZIP = ".zip";
    }
}
