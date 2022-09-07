namespace Doppler.BulkSender.Classes.Enums
{
    public enum MailStatus
    {
        OK = 0,
        Invalid = 1,
        RecipientRejected = 2,
        TimeOut = 3,
        TransactionError = 4,
        ServerRejected = 5,
        MailRejected = 6,
        MXNotFound = 7,
        InvalidEmail = 8,
        DelayedBounce = 9
    }
}
