using StockScanner.Model;

public class StockAnalyzer
{
    private const double MinMarketCap = 2_000_000_000; // 2B

    public AnalysisResult Analyze(StockData stock)
    {
        var result = new AnalysisResult { Stock = stock };

        // --- Hard rejects (sanity) ---
        if (stock.MarketCap < MinMarketCap) return Reject(result, "Market Cap < 2B (SAFE MODE)");

        if (stock.ForwardPE <= 0) return Reject(result, "Invalid Forward PE");
        if (stock.Ebitda <= 0) return Reject(result, "Negative / Missing EBITDA");
        if (stock.PegRatio <= 0) return Reject(result, "Invalid PEG");

        // --- Hard rejects (growth) ---
        if (!stock.IsRevenueGrowing3Y) return Reject(result, "Revenue NOT growing (need 3Y uptrend from 2022)");
        if (!stock.IsNetIncomeGrowing3Y) return Reject(result, "Net Income NOT growing (need 3Y uptrend from 2022)");

        if (stock.EpsGrowth5Y < 0.08) return Reject(result, "Low EPS growth outlook (< 8%)");
        if (stock.PegRatio > 1.2) return Reject(result, "PEG > 1.2 (overvalued vs growth)");

        // --- Score (soft rules) ---
        int score = 0;

        score += 20; 
        score += 20;

        // Growth/valuation
        if (stock.EpsGrowth5Y >= 0.10) score += 15;
        if (stock.PegRatio < 1.0) score += 15;

        // Valuation anchors
        if (stock.EvToEbitda > 0 && stock.EvToEbitda <= 12) score += 5;
        if (stock.EvToEbitda > 0 && stock.EvToEbitda <= 10) score += 5; // total 10
        if (stock.ForwardPE > 0 && stock.ForwardPE <= 20) score += 5;

        // Quality
        if (stock.ReturnOnEquity >= 0.20) score += 10;
        else if (stock.ReturnOnEquity >= 0.15) score += 5;

        // Debt safety
        if (stock.NetDebtToEbitda < 2.0) score += 10;
        else if (stock.NetDebtToEbitda < 3.0) score += 5;

        // Cash safety (prefer FCF)
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

    private AnalysisResult Reject(AnalysisResult result, string reason)
    {
        result.IsRejected = true;
        result.Score = 0;
        result.Recommendation = "REJECT";
        result.Reasons.Add(reason);
        return result;
    }
}