using Doppler.BulkSender.Classes;
using Doppler.BulkSender.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BulkSender.Processors
{
    public class ApiProcessorGireProducer : ApiProcessorProducer
    {
        public ApiProcessorGireProducer(IAppConfiguration configuration) : base(configuration)
        {
        }

        protected override List<CustomHeader> GetHeaderList(string[] headersArray)
        {
            var headerList = new List<CustomHeader>();

            for (int i = 0; i < headersArray.Length; i++)
            {
                if (headerList.Exists(h => h.Position == i))
                {
                    continue;
                }

                string index = string.Empty;
                bool numeric = false;
                foreach (char c in headersArray[i])
                {
                    if (char.IsNumber(c))
                    {
                        numeric = true;
                        index += c;
                    }
                    else if (numeric)
                    {
                        break;
                    }
                }
                var customHeader = new CustomHeader()
                {
                    Position = i,
                    HeaderName = headersArray[i],
                    Index = string.IsNullOrEmpty(index) ? -1 : int.Parse(index),
                    Name = string.IsNullOrEmpty(index) ? headersArray[i] : headersArray[i].Replace(index, string.Empty)
                };
                headerList.Add(customHeader);

                for (int k = i + 1; k < headersArray.Length; k++)
                {
                    if (headersArray[k].Contains(customHeader.Name))
                    {
                        int intIndex;
                        if (int.TryParse(headersArray[k].Replace(customHeader.Name, string.Empty), out intIndex))
                        {
                            var newHeader = new CustomHeader()
                            {
                                Position = k,
                                HeaderName = headersArray[k],
                                Index = intIndex,
                                Name = customHeader.Name
                            };
                            headerList.Add(newHeader);
                        }
                    }
                }
            }

            foreach (string name in headerList.Select(h => h.Name).Distinct())
            {
                int count = headerList.Where(h => h.Name == name).Count();
                headerList.Where(h => h.Name == name).ToList().ForEach(h => h.NameCount = count);
            }

            return headerList;
        }

        protected override void FillRecipientCustoms(ApiRecipient recipient, string[] data, List<CustomHeader> headerList, List<FieldConfiguration> fields)
        {
            var auxObject = new Dictionary<int, object>();

            for (int i = 0; i < data.Length; i++)
            {
                if (fields.Exists(f => f.Position == i && f.IsBasic) || string.IsNullOrEmpty(data[i]))
                {
                    continue;
                }

                CustomHeader customHeader = headerList.First(h => h.Position == i);

                if (customHeader != null)
                {
                    if (customHeader.NameCount == 1)
                    {
                        recipient.Fields.Add(customHeader.HeaderName, data[i]);
                    }
                    else
                    {
                        List<Dictionary<string, object>> list;

                        if (!recipient.Fields.ContainsKey("list"))
                        {
                            list = new List<Dictionary<string, object>>();
                            recipient.Fields.Add("list", list);
                        }
                        else
                        {
                            list = recipient.Fields["list"] as List<Dictionary<string, object>>;
                        }

                        Dictionary<string, object> item;
                        if (auxObject.ContainsKey(customHeader.Index))
                        {
                            item = auxObject[customHeader.Index] as Dictionary<string, object>;
                        }
                        else
                        {
                            item = new Dictionary<string, object>();
                            auxObject.Add(customHeader.Index, item);
                            list.Add(item);
                        }

                        item.Add(customHeader.Name, data[i]);
                        recipient.Fields.Add(customHeader.HeaderName, data[i]);
                    }
                }
            }
        }
    }
}
