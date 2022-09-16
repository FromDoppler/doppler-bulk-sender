using Doppler.BulkSender.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Doppler.BulkSender.Classes
{
    public class ReportBuffer
    {
        //el nombre del archivo de reporte es nombre_range_startdate.tmp
        //nombre es el nombre_nombreusuario
        private IAppConfiguration configuration;
        private IUserConfiguration userConfiguration;

        private Dictionary<long, DBStatusDto> currentBuffer;
        private DateTime currentStart;
        private DateTime currentEnd;
        private bool currentHasChanges;

        private DateTime start;
        private DateTime end;

        private int range;

        private int readCount;
        private DateTime readStart;
        private bool readMode;

        public ReportBuffer(IAppConfiguration configuration, IUserConfiguration userConfiguration, DateTime date, int range, int offset)
        {
            this.configuration = configuration;
            this.userConfiguration = userConfiguration;
            this.end = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
            this.start = this.currentStart = end.AddHours(-offset);
            this.range = range;
            readCount = 0;
            readMode = false;

            CreateOrUpdateBuffer();
            Reset();
        }

        public DateTime GetStartDate()
        {
            return this.currentStart;
        }

        public DateTime GetEndDate()
        {
            return this.end;
        }

        public void CreateOrUpdateBuffer()
        {
            string path = GetBufferPath();

            //TODO mover esto al file path helper cuando separemos reportes
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            List<string> files = Directory.GetFiles(path, "*.tmp").ToList();

            if (!files.Any())
            {
                CreateBufferFiles(start);

                return;
            }

            foreach (string file in files)
            {
                DateTime fileDate = GetDateFromFile(file);

                if (fileDate < start)
                {
                    File.Delete(file);
                }

                if (fileDate >= currentStart)
                {
                    currentStart = fileDate;
                }
            }

            currentEnd = currentStart.AddHours(range);

            if (end.Subtract(currentEnd).TotalHours > 24)
            {
                RemoveCompleteBuffer();

                currentStart = start;
                currentEnd = currentStart.AddHours(range);

                CreateBufferFiles(start);

                return;
            }

            //este es el final del utlimo buffer creado si es menor a end es que faltan crear.
            if (currentEnd < end)
            {
                currentStart = currentEnd;
                currentEnd = currentStart.AddHours(range);

                CreateBufferFiles(currentStart);
            }
        }

        public DBStatusDto Read()
        {
            if (!readMode)
            {
                LoadBufferByDate(readStart);
            }

            while (currentBuffer != null)
            {
                if (readCount < currentBuffer.Count)
                {
                    DBStatusDto item = currentBuffer.ElementAt(readCount).Value;

                    readCount++;

                    return item;
                }

                readStart = readStart.AddHours(range);

                LoadBufferByDate(readStart);

                readCount = 0;
            }

            return null;
        }

        public bool HasMoreItems()
        {
            if (!readMode)
            {
                LoadBufferByDate(readStart);
            }

            return currentBuffer != null && currentBuffer.Count > readCount;
        }

        public void Reset()
        {
            readCount = 0;
            readStart = start;
            readMode = false;
        }

        private void CreateBufferFiles(DateTime from)
        {
            while (from < end)
            {
                string fileName = GetFileName(from);
                string filePath = $@"{GetBufferPath()}\{fileName}";

                var reportAux = new Dictionary<long, DBStatusDto>();

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    new BinaryFormatter().Serialize(fileStream, reportAux);
                }

                from = from.AddHours(range);
            }

        }

        //esta va a ser la fecha de inicio de cada parte del buffer
        private DateTime GetDateFromFile(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);

            string date = name.Substring(name.Length - 10);

            return DateTime.ParseExact(date, "yyyyMMddHH", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void LoadBufferByDate(DateTime date)
        {
            if (currentBuffer != null && date >= currentStart && date < currentEnd)
            {
                return;
            }

            List<string> files = Directory.GetFiles(GetBufferPath(), "*.tmp").ToList();

            if (!files.Any())
            {
                return;
            }

            foreach (string file in files)
            {
                DateTime fileDate = GetDateFromFile(file);

                if (date >= fileDate && date < fileDate.AddHours(range))
                {
                    currentBuffer = LoadBufferFromDisk(file);
                    currentStart = fileDate;
                    currentEnd = fileDate.AddHours(range);
                    currentHasChanges = false;

                    return;
                }
            }

            currentBuffer = null;
        }

        private string GetFileName(DateTime date)
        {
            return $"buffer_{userConfiguration.Name}_{range}_{date.Year:0000}{date.Month:00}{date.Day:00}{date.Hour:00}.tmp";
        }

        private void SaveCurrentBufferToDisk()
        {
            if (!currentHasChanges)
            {
                return;
            }

            string filename = GetFileName(this.currentStart);

            string filePath = $@"{GetBufferPath()}\{filename}";

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                new BinaryFormatter().Serialize(fileStream, currentBuffer);
            }
        }

        private Dictionary<long, DBStatusDto> LoadBufferFromDisk(string fileName)
        {
            try
            {
                using (FileStream fileStream = File.OpenRead(fileName))
                {
                    return new BinaryFormatter().Deserialize(fileStream) as Dictionary<long, DBStatusDto>;
                }
            }
            catch (Exception e)
            {
                RemoveCompleteBuffer();

                throw new Exception($"ERROR TO LOAD BUFFER DELETE AND RESTART:{e}");
            }
        }

        private void RemoveCompleteBuffer()
        {
            List<string> files = Directory.GetFiles(GetBufferPath(), "*.tmp").ToList();

            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        public void AddItemsOrder(List<DBStatusDto> items)
        {
            if (!items.Any())
            {
                return;
            }

            //cargo el primer buffer para buscar de ahi
            LoadBufferByDate(start);

            int i = 0;

            while (i < items.Count)
            {
                var item = items[i];

                if (item.CreatedAt < start)
                {
                    i++;
                    continue;
                }

                if (item.CreatedAt >= currentStart && item.CreatedAt < currentEnd)
                {
                    if (currentBuffer.ContainsKey(item.DeliveryId))
                    {
                        currentBuffer[item.DeliveryId] = item;
                    }
                    else
                    {
                        currentBuffer.Add(item.DeliveryId, item);
                    }
                    currentHasChanges = true;

                    i++;

                    continue;
                }

                if (item.CreatedAt >= currentEnd && currentEnd < end)
                {
                    //cargo el siguiente buffer y no cuento porque vuelvo a comparar;
                    SaveCurrentBufferToDisk();

                    LoadBufferByDate(this.currentEnd);

                    continue;
                }
                else
                {
                    //aca habria que loguear a ver si entra
                    //ahora lo salteo. hay que ver si vuelvo todo para atras el buffer
                    i++;
                    System.Diagnostics.Debug.WriteLine("ENTRE DONDE NO TENIA QUE ENTRAR!!");
                }
            }

            SaveCurrentBufferToDisk();
        }

        private string GetBufferPath()
        {
            string path = new FilePathHelper(configuration, userConfiguration.Name).GetReportsFilesFolder();

            return $@"{path}\temp";
        }
    }
}
