using Doppler.BulkSender.Configuration;

namespace Doppler.BulkSender.Configuration
{
    public class CustomFieldConfiguration : FieldConfiguration
    {
        public object GetValue(string data)
        {
            return $"XXXX-{data.Substring(data.Length - 4)}";
        }
    }
}
