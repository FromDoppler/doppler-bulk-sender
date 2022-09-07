using System;

namespace Doppler.BulkSender.Classes
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string value, StringComparison stringComparison)
        {
            if (source == null)
            {
                return false;
            }

            return source.IndexOf(value, stringComparison) >= 0;
        }
    }
}
