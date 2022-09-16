using Doppler.BulkSender.Processors.Errors;
using Doppler.BulkSender.Classes;
using System;

namespace Doppler.BulkSender.Processors.Errors
{
    public enum ErrorType
    {
        DOWNLOAD = 1,
        UNZIP = 2,
        LOGIN = 3,
        PROCESS = 4,
        REPEATED = 5,
        UNEXPECTED = 6,
        DELIVERY = 7
    }
    public class ProcessError : IError
    {
        public int LineNumber { get; set; }
        public ErrorType Type { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }

        public string GetErrorLine()
        {
            string line = $"{Date}: {Message}";

            if (!string.IsNullOrEmpty(Description))
            {
                line += $" ({Description})";
            }

            if (LineNumber != 0)
            {
                line += $" processing line:{LineNumber}";
            }

            return line;
        }

        public string GetErrorLineResult(char separator, int maxlenght)
        {
            string line = string.Empty;

            //TODO: usar herencia en lugar de tipos de errores y cada uno define su metodo.
            switch (Type)
            {
                case ErrorType.PROCESS:
                    line = $"{Message}{separator}{separator}{separator}";
                    break;
                case ErrorType.DELIVERY:
                    string error = $"Send Fail: {Message}";

                    if (!string.IsNullOrEmpty(Description))
                    {
                        error = $"{error} ({Description})";
                    }

                    if (maxlenght > 0 && error.Length > maxlenght)
                    {
                        error = error.Substring(0, maxlenght);
                    }

                    line = $"{Constants.PROCESS_RESULT_OK}{separator}{error}{separator}{separator}";
                    break;
            }

            return line;
        }

        public void Process()
        {

        }
    }
}
