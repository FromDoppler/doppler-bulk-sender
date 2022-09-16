using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Configuration
{
    public class JoinedFieldConfiguration : FieldConfiguration
    {
        public char FieldSeparator { get; set; }
        public char KeyValueSeparator { get; set; }
    }
}
