using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Yahoo.Finance
{
    public class QuoteDataStore
    {
        private string _Symbol;
        private JObject QuoteObj;

        public QuoteDataStore(string symbol, JObject obj)
        {
            _Symbol = symbol;
            QuoteObj = obj;
        }

        public string Symbol
        {
            get
            {
                return _Symbol;
            }
        }

        #region "Extracted properties"

        public float? Open
        {
            get
            {
                try
                {
                    JObject obj = JObject.Parse(QuoteObj.Property("regularMarketOpen").Value.ToString());
                    float val = Convert.ToSingle(obj.Property("raw").Value.ToString());
                    return val;
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? YearRangeLow
        {
            get
            {
                try
                {
                    JObject obj = JObject.Parse(QuoteObj.Property("fiftyTwoWeekRange").Value.ToString());
                    JProperty prop = obj.Property("raw");
                    string propVal = prop.Value.ToString();
                    int loc1 = propVal.IndexOf(" ");
                    int loc2 = propVal.IndexOf(" ", loc1 + 1);
                    float Low = Convert.ToSingle(propVal.Substring(0, loc1 - 1));
                    float High = Convert.ToSingle(propVal.Substring(loc2 + 1));
                    return Low;
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? YearRangeHigh
        {
            get
            {
                try
                {
                    JObject obj = JObject.Parse(QuoteObj.Property("fiftyTwoWeekRange").Value.ToString());
                    JProperty prop = obj.Property("raw");
                    string propVal = prop.Value.ToString();
                    int loc1 = propVal.IndexOf(" ");
                    int loc2 = propVal.IndexOf(" ", loc1 + 1);
                    float Low = Convert.ToSingle(propVal.Substring(0, loc1 - 1));
                    float High = Convert.ToSingle(propVal.Substring(loc2 + 1));
                    return High;
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? SharesOutstanding
        {
            get
            {
                try
                {
                    JObject obj = JObject.Parse(QuoteObj.Property("sharesOutstanding").Value.ToString());
                    JProperty prop = obj.Property("raw");
                    float ToReturn = Convert.ToSingle(prop.Value.ToString());
                    return ToReturn;
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? DayHigh
        {
            get
            {
                try
                {
                    return Convert.ToSingle(GetRawValueFromChildObject("regularMarketDayHigh"));
                }
                catch
                {
                    return null;
                }
            }
        }

        public string ShortName
        {
            get
            {
                try
                {
                    return QuoteObj.Property("shortName").Value.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public string LongName
        {
            get
            {
                try
                {
                    return QuoteObj.Property("longName").Value.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? DayChange
        {
            get
            {
                try
                {
                    return Convert.ToSingle(GetRawValueFromChildObject("regularMarketChange"));
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? PreviousClose
        {
            get
            {
                try
                {
                    return Convert.ToSingle(GetRawValueFromChildObject("regularMarketPreviousClose"));
                }
                catch
                {
                    return null;
                }
            }
        }

        public string ExchangeTimezone
        {
            get
            {
                try
                {
                    return QuoteObj.Property("exchangeTimezoneShortName").Value.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? DayLow
        {
            get
            {
                try
                {
                    return Convert.ToSingle(GetRawValueFromChildObject("regularMarketDayLow"));
                }
                catch
                {
                    return null;
                }
            }
        }

        public string Currency
        {
            get
            {
                try
                {
                    return QuoteObj.Property("currency").Value.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public float? Price
        {
            get
            {
                try
                {
                    return Convert.ToSingle(GetRawValueFromChildObject("regularMarketPrice"));
                }   
                catch
                {
                    return null;
                }
            }
        }

        public float? Volume
        {
            get
            {
                try
                {
                    return Convert.ToSingle(GetRawValueFromChildObject("regularMarketVolume"));
                }   
                catch
                {
                    return null;
                }
            }
        }

        public float? MarketCap
        {
            get
            {
                try
                {
                    return Convert.ToSingle(GetRawValueFromChildObject("marketCap"));
                }   
                catch
                {
                    return null;
                }
            }
        }

        public string ExchangeName
        {
            get
            {
                try
                {
                    return QuoteObj.Property("fullExchangeName").Value.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public string QuoteType
        {
            get
            {
                try
                {
                    return QuoteObj.Property("quoteType").Value.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        private string GetRawValueFromChildObject(string PropertyName)
        {
            JProperty propObj = QuoteObj.Property(PropertyName);
            if (propObj == null)
            {
                throw new Exception("Unable to find property '" + PropertyName + "' in quote data.");
            }
            if (propObj.Type != JTokenType.Property)
            {
                throw new Exception("Property '" + PropertyName + "' was not a child object.");
            }
            JObject ob = JObject.Parse(propObj.Value.ToString());
            JProperty propRaw = ob.Property("raw");
            if (propRaw == null)
            {
                throw new Exception("Property labeled 'raw' does not exist in the specified child object.");
            }
            return propRaw.Value.ToString();
        }

        #endregion


        #region "static constructors"

        public static QuoteDataStore[] ExtractQuoteDataStoresFromWebPage(string web_page_html)
        {
            int loc1 = web_page_html.IndexOf("root.App.main =");
            if (loc1 == -1)
            {
                throw new Exception("Unable to location data store in web page content.");
            }
            loc1 = web_page_html.IndexOf("{", loc1 + 1) - 1; //Get space BEFORE the opening bracket (hence -1)

            int loc2 = web_page_html.IndexOf("</script><script>", loc1);
            loc2 = web_page_html.LastIndexOf(";", loc2 - 1);
            loc2 = web_page_html.LastIndexOf(";", loc2 - 1); //Last semicolon before last closing bracket

            //Get the JSON body
            string FullJson = web_page_html.Substring(loc1 + 1, loc2 - loc1 - 1);
            return ExtractQuoteDataStoresFromRootJson(FullJson);
        }

        public static QuoteDataStore[] ExtractQuoteDataStoresFromRootJson(string root_json)
        {
            JObject Master = JObject.Parse(root_json);

            //Step down to the quoteData
            JObject jo_context = JObject.Parse(Master.Property("context").Value.ToString());
            JObject jo_dispatcher = JObject.Parse(jo_context.Property("dispatcher").Value.ToString());
            JObject jo_stores = JObject.Parse(jo_dispatcher.Property("stores").Value.ToString());
            JObject jo_StreamDataStore = JObject.Parse(jo_stores.Property("StreamDataStore").Value.ToString());
            JObject jo_quoteData = JObject.Parse(jo_StreamDataStore.Property("quoteData").Value.ToString());

            //Get each quote object
            IEnumerable<JProperty> QuoteProps = jo_quoteData.Properties();

            //Create each
            List<QuoteDataStore> ToReturn = new List<QuoteDataStore>();
            foreach (JProperty prop in QuoteProps)
            {
                JObject thisObj = JObject.Parse(prop.Value.ToString());
                ToReturn.Add(new QuoteDataStore(prop.Name, thisObj));
            }

            return ToReturn.ToArray();      
        }

        #endregion


    }
}