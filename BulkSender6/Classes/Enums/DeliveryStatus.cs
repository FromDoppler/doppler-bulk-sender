namespace Doppler.BulkSender.Classes.Enums
{
    public enum DeliveryStatus
    {
        Queued = 0,
        Sent = 1,
        Rejected = 2,
        Retrying = 3,
        Invalid = 4,
        Dropped = 5
    }
}
