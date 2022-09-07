using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Doppler.BulkSender.Reports
{
    public class EdesurReport : ReportBase
    {
        private StringBuilder _stringBuilder;
        public string Separator { get; set; }

        public EdesurReport()
        {
            _stringBuilder = new StringBuilder();
        }

        //protected void FillItems()
        //{
        //    Dictionary<string, int> headers;
        //    List<Dictionary<string, int>> headersList;
        //    try
        //    {
        //        foreach (string file in SourceFiles)
        //        {
        //            using (var reader = new StreamReader(file))
        //            {
        //                int processedIndex;
        //                int resultIndex;

        //                List<string> fileHeaders = reader.ReadLine().Split(Separator).ToList();

        //                headers = GetHeadersIndexes(_reportConfiguration.ReportFields, fileHeaders, out processedIndex, out resultIndex);

        //                //headersList = GetHeadersList(_reportConfiguration.Fields.Except(headers.Keys).ToList(), fileHeaders);
        //                List<string> fields = _reportConfiguration.ReportFields.Where(x => !string.IsNullOrEmpty(x.NameInFile)).Select(x => x.HeaderName).Except(headers.Keys).ToList();
        //                headersList = GetHeadersList(fields, fileHeaders);

        //                while (!reader.EndOfStream)
        //                {
        //                    string[] lineArray = reader.ReadLine().Split(Separator);

        //                    if (processedIndex == -1 || resultIndex == -1 || lineArray.Length <= resultIndex)
        //                    {
        //                        continue;
        //                    }

        //                    if (lineArray[processedIndex] != Constants.PROCESS_RESULT_OK)
        //                    {
        //                        continue;
        //                    }

        //                    string resultId = lineArray[resultIndex];

        //                    foreach (var dHeader in headersList)
        //                    {
        //                        bool hasValue = false;
        //                        var item = new ReportItem();
        //                        foreach (int specialValue in dHeader.Values)
        //                        {
        //                            string fileValue = lineArray[specialValue];
        //                            if (!string.IsNullOrEmpty(fileValue))
        //                            {
        //                                item.AddValue(fileValue.Trim());
        //                                hasValue = true;
        //                            }
        //                        }

        //                        if (hasValue)
        //                        {
        //                            foreach (int fixValue in headers.Values)
        //                            {
        //                                item.AddValue(lineArray[fixValue].Trim(), fixValue);
        //                            }
        //                            item.ResultId = resultId;
        //                            _items.Add(item);
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        GetDataFromDB(_items, _dateFormat);
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError("Error trying to get report items");
        //        throw;
        //    }
        //}

        private List<Dictionary<string, int>> GetHeadersList(List<string> fields, List<string> fileHeaders)
        {
            var headerDictionaryList = new List<Dictionary<string, int>>();

            int index = 1;
            int i = 0;
            int j = 0;
            while (i < fileHeaders.Count)
            {
                var headerDictionary = new Dictionary<string, int>();
                foreach (string field in fields)
                {
                    string headerNumber = $"{index}{field}";
                    j = i;
                    while (j < fileHeaders.Count)
                    {
                        string fileHeader = fileHeaders[j];
                        if (fileHeader.Contains(headerNumber))
                        {
                            headerDictionary.Add(field, j);
                            j++;
                            break;
                        }
                        j++;
                    }
                }

                if (headerDictionary.Count > 0)
                {
                    headerDictionaryList.Add(headerDictionary);
                }
                index++;
                i = j;
            }

            return headerDictionaryList;
        }

        protected override void FillReport()
        {
            string headerLine = string.Join(Separator.ToString(), _headerList);

            _stringBuilder.AppendLine(headerLine);

            foreach (ReportItem item in _items)
            {
                string itemLine = string.Join(Separator.ToString(), item.GetValues());
                _stringBuilder.AppendLine(itemLine);
            }
        }

        protected override void Save()
        {
            _reportFileName = $@"{ReportPath}\{ReportName}";

            using (var streamWriter = new StreamWriter(_reportFileName))
            {
                streamWriter.Write(_stringBuilder.ToString());
            }
        }
    }
}
