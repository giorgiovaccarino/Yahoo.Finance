using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using TimHanewichToolkit;
using System.Globalization;

namespace Yahoo.Finance
{
    public class HistoricalDataProvider
    {
        public HistoricalDataRecord[] HistoricalData { get; set; }
        public HistoricalDataDownloadResult DownloadResult { get; set; }
        NumberFormatInfo nfi = NumberFormatInfo.InvariantInfo;
        DateTimeFormatInfo dfi = DateTimeFormatInfo.InvariantInfo;

        public async Task DownloadHistoricalDataAsync(string StockSymbol, DateTime PeriodStart, DateTime PeriodEnd, int try_count = 10)
        {
            //Set up
            HistoricalData = null;
            DownloadResult = HistoricalDataDownloadResult.Downloading;


            //Get try count to use
            int trycountToUse = 10;
            if (try_count > 0)
            {
                trycountToUse = try_count;
            }

            //Get the data
            int HaveTriedCount = 0;
            while (DownloadResult != HistoricalDataDownloadResult.Successful && HaveTriedCount < try_count && DownloadResult != HistoricalDataDownloadResult.DataDoesNotExistForSpecifiedTimePeriod)
            {
                await TryGetHistoricalDatAsync(StockSymbol, PeriodStart, PeriodEnd);
                HaveTriedCount = HaveTriedCount + 1;
            }
        }

        private async Task TryGetHistoricalDatAsync(string symbol, DateTime start, DateTime end)
        {
            DateTime RequestStart = DateTime.Now;

            //Get the crumb!
            HttpClient hc = new HttpClient();
            HttpResponseMessage rm = await hc.GetAsync("https://finance.yahoo.com/quote/" + symbol);
            string web = await rm.Content.ReadAsStringAsync();
            int loc1 = web.IndexOf("}}}}},\"CrumbStore\":{\"crumb\":");
            if (loc1 == -1)
            {
                throw new Exception("Unable to verify stock '" + symbol + "'.");
            }
            loc1 = web.IndexOf("crumb", loc1 + 1);
            loc1 = web.IndexOf(":", loc1 + 1);
            loc1 = web.IndexOf("\"", loc1 + 1);
            int loc2 = web.IndexOf("\"", loc1 + 1);
            string crumb = web.Substring(loc1 + 1, loc2 - loc1 - 1);

            //Get the unix times
            string Unix1 = UnixToolkit.GetUnixTime(start).ToString();
            string Unix2 = UnixToolkit.GetUnixTime(end).ToString();

            //Get the info
            string urlfordata = "https://query1.finance.yahoo.com/v7/finance/download/" + symbol + "?period1=" + Unix1 + "&period2=" + Unix2 + "&interval=1d&events=history&crumb=" + crumb;
            HttpResponseMessage fr = await hc.GetAsync(urlfordata);
            string resptext = await fr.Content.ReadAsStringAsync();

            //Show the error if one was encountered
            if (fr.StatusCode != System.Net.HttpStatusCode.OK)
            {
                HistoricalData = null;

                //A bad request would be shown if for example the data does not exist for this stock for those dates.
                if (fr.StatusCode == System.Net.HttpStatusCode.BadRequest) //We requested data that doesnt exist. For example, getting BYND (beyond meat) data for 2015: 400 Bad Request: Data doesn't exist for startDate = 1431746344, endDate = 1437016744
                {
                    DownloadResult = HistoricalDataDownloadResult.DataDoesNotExistForSpecifiedTimePeriod;
                }
                else if (fr.StatusCode == System.Net.HttpStatusCode.Unauthorized) //Probably an 'Invalid cookie' message, unauthorized. So mark it as not authroized
                {
                    DownloadResult = HistoricalDataDownloadResult.Unauthorized;
                }
                else if (fr.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    DownloadResult = HistoricalDataDownloadResult.NoDataFound;
                }
                else
                {
                    DownloadResult = HistoricalDataDownloadResult.OtherFailure;
                }

                return; //Exit
            }

            //Parse into data records
            List<HistoricalDataRecord> datarecs = new List<HistoricalDataRecord>();
            List<string> Splitter = new List<string>();
            Splitter.Add("\n");
            string[] rows = resptext.Split(Splitter.ToArray(), StringSplitOptions.None);
            int t = 0;
            for (t = 1; t <= rows.Length - 1; t++)
            {
                string thisrow = rows[t];
                if (thisrow != "")
                {
                    try
                    {
                        HistoricalDataRecord rec = new HistoricalDataRecord();

                        Splitter.Clear();
                        Splitter.Add(",");
                        string[] cols = thisrow.Split(Splitter.ToArray(), StringSplitOptions.None);

                        rec.Date = DateTime.Parse(cols[0], dfi);
                        rec.Open = System.Convert.ToSingle(cols[1], nfi);
                        rec.High = System.Convert.ToSingle(cols[2], nfi);
                        rec.Low = System.Convert.ToSingle(cols[3], nfi);
                        rec.Close = System.Convert.ToSingle(cols[4], nfi);
                        rec.AdjustedClose = System.Convert.ToSingle(cols[5], nfi);
                        rec.Volume = System.Convert.ToInt32(cols[6], nfi);

                        datarecs.Add(rec);
                    }
                    catch
                    {
                        throw new Exception("Unable to conver this row: " + thisrow);
                    }
                }
            }

            //Post it to this class
            HistoricalData = datarecs.ToArray();
            DownloadResult = HistoricalDataDownloadResult.Successful;
        }

    }
}