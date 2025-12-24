using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using StockScanner.Clients;
using StockScanner.Model;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("[EVENT] INITIALIZING MID / LARGE / MEGA CAP SCANNER (SAFE MODE)...");
        Console.WriteLine("[INFO] Universe: Yahoo Largest Market Cap | Min Cap: 2B USD\n");

        var session = new YahooSession();
        await session.IniatilizeAsync();

        var client = new YahooClient(session);
        var analyzer = new StockAnalyzer();

        Console.WriteLine("[EVENT] Fetching Yahoo Largest Market Cap tickers...");
        var tickers = await TickerSource.GetYahooLargestMarketCapTickersAsync(
            session,
            targetCount: 3000
        );

        Console.WriteLine($"[EVENT] Loaded {tickers.Count} tickers.\n");

        var results = new ConcurrentBag<AnalysisResult>();

        int total = tickers.Count;
        int processed = 0;

        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("[EVENT] STARTING PARALLEL SCAN...");
        Console.WriteLine("[INFO] This may take a few minutes depending on network & rate limits\n");

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 16 
        };

        await Parallel.ForEachAsync(tickers, options, async (symbol, token) =>
        {
            try
            {
                var stock = await client.GetStockDataAsync(symbol);

                int current = System.Threading.Interlocked.Increment(ref processed);
                if (current % 25 == 0)
                    Console.Write(".");

                if (stock != null)
                {
                    var analysis = analyzer.Analyze(stock);
                    results.Add(analysis);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[WARN] {symbol}: {ex.Message}");
            }
        });

        stopwatch.Stop();

        Console.WriteLine($"\n\n[EVENT] SCAN COMPLETE in {stopwatch.Elapsed.TotalSeconds:F0}s");
        Console.WriteLine($"[EVENT] Processed {processed} / {total} tickers\n");

        // --- Sort & filter ---
        var bestStocks = results
            .Where(r => !r.IsRejected)
            .OrderByDescending(r => r.Score)
            .ToList();

        Console.WriteLine($"[EVENT] FOUND {bestStocks.Count} INVESTABLE CANDIDATES\n");

        PrintHeader();

        foreach (var result in bestStocks)
        {
            PrintRow(result);
        }

        Console.WriteLine("\n[EVENT] DONE. Press any key to exit.");
        Console.ReadKey();
    }
    

    static void PrintHeader()
    {
        Console.WriteLine(
            $"{"Symbol",-8} | {"Score",-5} | {"Rec",-12} | {"PEG",-5} | {"EPS 5Y",-7} | {"ROE",-6} | {"Fwd PE",-6}"
        );
        Console.WriteLine(new string('-', 85));
    }

    static void PrintRow(AnalysisResult r)
    {
        if (r.Recommendation == "STRONG BUY")
            Console.ForegroundColor = ConsoleColor.Green;
        else if (r.Recommendation == "BUY")
            Console.ForegroundColor = ConsoleColor.Cyan;
        else if (r.Recommendation == "WATCHLIST")
            Console.ForegroundColor = ConsoleColor.Yellow;
        else
            Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine(
            $"{r.Stock.Symbol,-8} | " +
            $"{r.Score,-5} | " +
            $"{r.Recommendation,-12} | " +
            $"{r.Stock.PegRatio,-5:F2} | " +
            $"{r.Stock.EpsGrowth5Y,-7:P0} | " +
            $"{r.Stock.ReturnOnEquity,-6:P0} | " +
            $"{r.Stock.ForwardPE,-6:F1}"
        );

        Console.ResetColor();
    }
}