namespace Doppler.BulkSender.Configuration
{
    public class FieldConfiguration
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public string Type { get; set; }
        public bool IsBasic { get; set; }
        public bool IsKey { get; set; }
        public bool IsForList { get; set; }
        public bool IsAttachment { get; set; }
    }
}
