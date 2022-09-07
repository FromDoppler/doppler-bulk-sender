using System;
using System.Collections.Generic;

namespace Doppler.BulkSender.Classes
{
    public class Link
    {
        public string href { get; set; }
        public string description { get; set; }
        public string rel { get; set; }
    }

    public class ApiResponse
    {
        public Guid createdResourceId { get; set; }
        public string message { get; set; }
        public List<Link> _links { get; set; }
    }
}