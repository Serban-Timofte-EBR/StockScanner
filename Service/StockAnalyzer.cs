using System;
using StockScanner.Model;

public class StockAnalyzer
{
    private readonly double _minMarketCap;
    private readonly bool _excludeOtc;
    private readonly bool _excludeChina;

    public StockAnalyzer(double minMarketCap = 2_000_000_000, bool excludeOtc = true, bool excludeChina = false)
    {
        _minMarketCap = minMarketCap;
        _excludeOtc = excludeOtc;
        _excludeChina = excludeChina;
    }

    public AnalysisResult Analyze(StockData stock)
    {
        var result = new AnalysisResult { Stock = stock };

        if (stock.MarketCap < _minMarketCap) return Reject(result, "Market Cap below threshold");
        if (!IsAllowedGlobalExchange(stock.Exchange)) return Reject(result, "Exchange not allowed");
        if (_excludeChina && IsChina(stock.Country)) return Reject(result, "China excluded");

        if (!IsFinitePositive(stock.ForwardPE)) return Reject(result, "Invalid Forward PE");
        if (stock.Ebitda <= 0) return Reject(result, "Invalid EBITDA");
        if (!IsFinitePositive(stock.PegRatio)) return Reject(result, "Invalid PEG");
        if (stock.PegRatio > 1.2) return Reject(result, "PEG > 1.2");

        if (!IsFinite(stock.EpsGrowth5Y) || stock.EpsGrowth5Y < 0.08) return Reject(result, "EPS growth < 8%");
        if (stock.EpsGrowth5Y > 1.0) return Reject(result, "EPS growth outlier");

        if (!stock.IsRevenueGrowing3Y) return Reject(result, "Revenue not growing 3Y");
        if (!stock.IsNetIncomeGrowing3Y) return Reject(result, "Net income not growing 3Y");

        if (!IsFinite(stock.ReturnOnEquity) || stock.ReturnOnEquity <= 0) return Reject(result, "ROE <= 0");
        if (!IsFinite(stock.EvToEbitda) || stock.EvToEbitda < 0) return Reject(result, "Invalid EV/EBITDA");

        int score = 0;

        score += 20;
        score += 20;

        if (stock.EpsGrowth5Y >= 0.10) score += 15;
        if (stock.PegRatio < 1.0) score += 15;

        if (stock.EvToEbitda > 0 && stock.EvToEbitda <= 12) score += 5;
        if (stock.EvToEbitda > 0 && stock.EvToEbitda <= 10) score += 5;
        if (stock.ForwardPE <= 20) score += 5;

        if (stock.ReturnOnEquity >= 0.20) score += 10;
        else if (stock.ReturnOnEquity >= 0.15) score += 5;

        if (stock.NetDebtToEbitda < 2.0) score += 10;
        else if (stock.NetDebtToEbitda < 3.0) score += 5;

        if (stock.FreeCashFlow > 0) score += 10;
        else if (stock.CurrentRatio >= 1.2) score += 5;

        result.Score = score;
        result.IsRejected = false;

        result.Recommendation =
            score >= 70 ? "STRONG BUY" :
            score >= 60 ? "BUY" :
            score >= 50 ? "WATCHLIST" :
            "HOLD";

        return result;
    }

    private bool IsAllowedGlobalExchange(string exchange)
    {
        if (string.IsNullOrWhiteSpace(exchange)) return false;
        var ex = exchange.Trim().ToLowerInvariant();

        if (_excludeOtc)
        {
            if (ex.Contains("otc")) return false;
            if (ex.Contains("pink")) return false;
            if (ex.Contains("otcqb") || ex.Contains("otcqx")) return false;
            if (ex == "pnk" || ex == "obb" || ex == "oqb") return false;
        }

        if (ex.Contains("nyse")) return true;
        if (ex.Contains("nasdaq")) return true;
        if (ex.Contains("nysearca")) return true;
        if (ex.Contains("amex")) return true;

        if (ex.Contains("london") || ex.Contains("lse")) return true;
        if (ex.Contains("xetra") || ex.Contains("frankfurt")) return true;
        if (ex.Contains("paris")) return true;
        if (ex.Contains("amsterdam")) return true;
        if (ex.Contains("swiss") || ex.Contains("zurich")) return true;
        if (ex.Contains("stockholm") || ex.Contains("oslo") || ex.Contains("copenhagen") || ex.Contains("helsinki")) return true;
        if (ex.Contains("madrid") || ex.Contains("milano") || ex.Contains("milan") || ex.Contains("brussels") || ex.Contains("vienna")) return true;

        if (ex.Contains("tokyo") || ex.Contains("osaka") || ex.Contains("japan")) return true;
        if (ex.Contains("hong kong")) return true;
        if (ex.Contains("singapore")) return true;
        if (ex.Contains("toronto") || ex.Contains("tsx")) return true;
        if (ex.Contains("australia") || ex.Contains("asx") || ex.Contains("sydney")) return true;

        if (ex == "nyq" || ex == "nms" || ex == "ngm" || ex == "ncm" || ex == "ase") return true;

        return false;
    }

    private static bool IsChina(string country)
    {
        if (string.IsNullOrWhiteSpace(country)) return false;
        var c = country.Trim().ToLowerInvariant();
        return c == "china" || c == "hong kong";
    }

    private static bool IsFinite(double v) => !(double.IsNaN(v) || double.IsInfinity(v));
    private static bool IsFinitePositive(double v) => IsFinite(v) && v > 0;

    private AnalysisResult Reject(AnalysisResult result, string reason)
    {
        result.IsRejected = true;
        result.Score = 0;
        result.Recommendation = "REJECT";
        result.Reasons.Add(reason);
        return result;
    }
}