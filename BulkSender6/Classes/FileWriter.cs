using System.IO;

namespace Doppler.BulkSender.Classes
{
    public class FileWriter
    {
        private readonly string filePath;
        private readonly object locker;

        public FileWriter(string filePath)
        {
            this.filePath = filePath;
            locker = new object();
        }

        public void AppendLine(string text)
        {
            lock (locker)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.WriteLine(text);
                }
            }
        }

        public void WriteFile(string text)
        {
            lock (locker)
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (StreamWriter streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(text);
                }
            }
        }
    }
}
