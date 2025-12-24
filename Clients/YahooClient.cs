using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using StockScanner.Clients;
using StockScanner.Model;

public class YahooClient
{
    private readonly YahooSession _session;

    public YahooClient(YahooSession session) => _session = session;

    public async Task<StockData?> GetStockDataAsync(string symbol)
    {
        var crumb = _session.GetCrumb();
        if (string.IsNullOrWhiteSpace(crumb)) return null;

        var url =
            $"https://query2.finance.yahoo.com/v10/finance/quoteSummary/{symbol}" +
            $"?modules=price,summaryDetail,financialData,defaultKeyStatistics,incomeStatementHistory,earningsTrend,summaryProfile" +
            $"&crumb={Uri.EscapeDataString(crumb)}";

        var response = await GetStringWithRetryAsync(url);
        if (string.IsNullOrWhiteSpace(response)) return null;

        var json = JObject.Parse(response);
        var result = json["quoteSummary"]?["result"]?.First;
        if (result == null) return null;

        var data = new StockData
        {
            Symbol = symbol,
            ShortName = ReadText(result, "price", "shortName") ?? symbol,

            Exchange = ReadText(result, "price", "exchangeName")
                       ?? ReadText(result, "price", "fullExchangeName")
                       ?? ReadText(result, "price", "exchange")
                       ?? "",

            QuoteType = ReadText(result, "price", "quoteType") ?? "",

            Currency = ReadText(result, "price", "currency")
                       ?? ReadText(result, "financialData", "financialCurrency")
                       ?? "",

            Country = ReadText(result, "summaryProfile", "country") ?? "",

            MarketCap = ReadLong(result, "price", "marketCap"),
            Price = ReadDouble(result, "financialData", "currentPrice"),

            PegRatio = ReadDouble(result, "defaultKeyStatistics", "pegRatio"),
            ForwardPE = ReadDouble(result, "defaultKeyStatistics", "forwardPE"),
            EvToEbitda = ReadDouble(result, "defaultKeyStatistics", "enterpriseToEbitda"),

            ReturnOnEquity = ReadDouble(result, "financialData", "returnOnEquity"),
            TotalCash = ReadLong(result, "financialData", "totalCash"),
            TotalDebt = ReadLong(result, "financialData", "totalDebt"),
            Ebitda = ReadLong(result, "financialData", "ebitda"),
            CurrentRatio = ReadDouble(result, "financialData", "currentRatio"),
            FreeCashFlow = ReadLong(result, "financialData", "freeCashflow")
        };

        if (data.ForwardPE <= 0)
            data.ForwardPE = ReadDouble(result, "summaryDetail", "forwardPE");

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

    private async Task<string?> GetStringWithRetryAsync(string url)
    {
        var http = _session.GetClient();

        for (int attempt = 1; attempt <= 7; attempt++)
        {
            using var resp = await http.GetAsync(url);

            if (resp.StatusCode == (HttpStatusCode)429 || (int)resp.StatusCode >= 500)
            {
                var delayMs = Math.Min(30_000, 400 * (int)Math.Pow(2, attempt)) + Random.Shared.Next(0, 250);
                await Task.Delay(delayMs);
                continue;
            }

            if (!resp.IsSuccessStatusCode)
                return null;

            return await resp.Content.ReadAsStringAsync();
        }

        return null;
    }

    private static double ReadDouble(JToken root, string module, string field)
    {
        var tok = root[module]?[field];
        if (tok == null) return 0.0;
        if (tok.Type == JTokenType.Object) return (double)(tok["raw"] ?? 0.0);
        return (double)(tok ?? 0.0);
    }

    private static long ReadLong(JToken root, string module, string field)
    {
        var tok = root[module]?[field];
        if (tok == null) return 0L;
        if (tok.Type == JTokenType.Object) return (long)(tok["raw"] ?? 0L);
        return (long)(tok ?? 0L);
    }

    private static string? ReadText(JToken root, string module, string field)
    {
        var tok = root[module]?[field];
        if (tok == null) return null;
        if (tok.Type == JTokenType.Object) return (string?)tok["raw"] ?? (string?)tok["fmt"];
        if (tok.Type == JTokenType.String) return (string?)tok;
        return tok.ToString();
    }
}