using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using StockScanner.Clients;
using StockScanner.Model;

public class YahooClient
{
    private readonly YahooSession _session;

    public YahooClient(YahooSession session) => _session = session;

    public async Task<StockData?> GetStockDataAsync(string symbol)
    {
        string crumb = _session.GetCrumb();
        if (string.IsNullOrEmpty(crumb)) return null;

        string url =
            $"https://query2.finance.yahoo.com/v10/finance/quoteSummary/{symbol}" +
            $"?modules=price,summaryDetail,financialData,defaultKeyStatistics,incomeStatementHistory,earningsTrend" +
            $"&crumb={crumb}";

        try
        {
            var response = await _session.GetClient().GetStringAsync(url);
            var json = JObject.Parse(response);
            var result = json["quoteSummary"]?["result"]?.First;
            if (result == null) return null;

            var data = new StockData
            {
                Symbol = symbol,
                ShortName = (string?)result["price"]?["shortName"] ?? symbol,

                MarketCap = GetLong(result, "price", "marketCap"),
                Price = GetDouble(result, "financialData", "currentPrice"),

                PegRatio = GetDouble(result, "defaultKeyStatistics", "pegRatio"),
                ForwardPE = GetDouble(result, "defaultKeyStatistics", "forwardPE"),
                EvToEbitda = GetDouble(result, "defaultKeyStatistics", "enterpriseToEbitda"),

                ReturnOnEquity = GetDouble(result, "financialData", "returnOnEquity"),
                TotalCash = GetLong(result, "financialData", "totalCash"),
                TotalDebt = GetLong(result, "financialData", "totalDebt"),
                Ebitda = GetLong(result, "financialData", "ebitda"),
                CurrentRatio = GetDouble(result, "financialData", "currentRatio"),
                FreeCashFlow = GetLong(result, "financialData", "freeCashflow")
            };

            if (data.ForwardPE <= 0)
                data.ForwardPE = GetDouble(result, "summaryDetail", "forwardPE");

            var trends = result["earningsTrend"]?["trend"] as JArray;
            if (trends != null)
            {
                foreach (var t in trends)
                {
                    var period = (string?)t["period"];
                    var growth = (double)(t["growth"]?["raw"] ?? 0.0);

                    if (period == "+5y" || period == "5y")
                    {
                        data.EpsGrowth5Y = growth;
                        break;
                    }
                    if (period == "+1y" && data.EpsGrowth5Y == 0)
                        data.EpsGrowth5Y = growth;
                }
            }

            if (data.PegRatio <= 0 && data.EpsGrowth5Y > 0 && data.ForwardPE > 0)
                data.PegRatio = data.ForwardPE / (data.EpsGrowth5Y * 100.0);

            var hist = result["incomeStatementHistory"]?["incomeStatementHistory"] as JArray;
            if (hist != null && hist.Count >= 3)
            {
                data.RevenueY0 = (long)(hist[0]?["totalRevenue"]?["raw"] ?? 0);
                data.RevenueY1 = (long)(hist[1]?["totalRevenue"]?["raw"] ?? 0);
                data.RevenueY2 = (long)(hist[2]?["totalRevenue"]?["raw"] ?? 0);

                data.NetIncomeY0 = (long)(hist[0]?["netIncome"]?["raw"] ?? 0);
                data.NetIncomeY1 = (long)(hist[1]?["netIncome"]?["raw"] ?? 0);
                data.NetIncomeY2 = (long)(hist[2]?["netIncome"]?["raw"] ?? 0);
            }

            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching {symbol}: {ex.Message}");
            return null;
        }
    }

    private static double GetDouble(JToken root, string module, string field)
        => (double)(root[module]?[field]?["raw"] ?? 0.0);

    private static long GetLong(JToken root, string module, string field)
        => (long)(root[module]?[field]?["raw"] ?? 0L);
}