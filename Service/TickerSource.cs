using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using StockScanner.Clients;

public static class TickerSource
{
    public static async Task<List<string>> GetYahooLargestMarketCapTickersAsync(
        YahooSession session,
        int targetCount = 3000,
        int pageSize = 250,
        string region = "US",
        string quoteType = "equity")
    {
        if (pageSize <= 0 || pageSize > 250) pageSize = 250;

        var client = session.GetClient();
        var crumb = session.GetCrumb();

        if (string.IsNullOrWhiteSpace(crumb))
            return new List<string>();

        var url = $"https://query2.finance.yahoo.com/v1/finance/screener?crumb={Uri.EscapeDataString(crumb)}&lang=en-US&region={Uri.EscapeDataString(region)}&formatted=false&corsDomain=finance.yahoo.com";

        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int offset = 0; symbols.Count < targetCount; offset += pageSize)
        {
            var payload = BuildPayload(quoteType, region, pageSize, offset);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
            req.Headers.TryAddWithoutValidation("Accept", "application/json");

            req.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

            using var resp = await client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"\n[ERROR] Screener HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}");
                Console.WriteLine($"[ERROR] Body (first 300 chars): {SafePrefix(body, 300)}");
                break;
            }

            var json = JObject.Parse(body);
            var quotes = json["finance"]?["result"]?.First?["quotes"] as JArray;

            if (quotes == null || quotes.Count == 0)
                break;

            foreach (var q in quotes)
            {
                var sym = (string?)q["symbol"];
                if (!string.IsNullOrWhiteSpace(sym))
                    symbols.Add(sym);
            }
        }

        return symbols.Take(targetCount).ToList();
    }

    private static JObject BuildPayload(string quoteType, string region, int size, int offset)
    {
        return new JObject
        {
            ["size"] = size,
            ["offset"] = offset,
            ["sortField"] = "intradaymarketcap",
            ["sortType"] = "desc",
            ["quoteType"] = quoteType,
            ["topOperator"] = "and",
            ["query"] = new JObject
            {
                ["operator"] = "and",
                ["operands"] = new JArray
                {
                    new JObject
                    {
                        ["operator"] = "or",
                        ["operands"] = new JArray
                        {
                            new JObject
                            {
                                ["operator"] = "eq",
                                ["operands"] = new JArray { "region", region.ToLowerInvariant() }
                            }
                        }
                    }
                }
            }
        };
    }

    private static string SafePrefix(string s, int n)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n));
}