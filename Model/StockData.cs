namespace StockScanner.Model;

public class StockData
{
    public string Symbol { get; set; } = "";
    public string ShortName { get; set; } = "";

    public string Exchange { get; set; } = "";
    public string QuoteType { get; set; } = "";
    public string Currency { get; set; } = "";
    public string Country { get; set; } = "";

    public double MarketCap { get; set; }
    public double Price { get; set; }

    public double ForwardPE { get; set; }
    public double PegRatio { get; set; }
    public double EvToEbitda { get; set; }

    public long RevenueY0 { get; set; }
    public long RevenueY1 { get; set; }
    public long RevenueY2 { get; set; }

    public long NetIncomeY0 { get; set; }
    public long NetIncomeY1 { get; set; }
    public long NetIncomeY2 { get; set; }

    public double EpsGrowth5Y { get; set; }

    public double ReturnOnEquity { get; set; }
    public double TotalCash { get; set; }
    public double TotalDebt { get; set; }
    public double Ebitda { get; set; }
    public double CurrentRatio { get; set; }
    public double FreeCashFlow { get; set; }

    public double NetDebt => TotalDebt - TotalCash;
    public double NetDebtToEbitda => Ebitda > 0 ? NetDebt / Ebitda : double.PositiveInfinity;

    public bool IsRevenueGrowing3Y =>
        RevenueY0 > 0 && RevenueY1 > 0 && RevenueY2 > 0 &&
        RevenueY0 > RevenueY1 && RevenueY1 > RevenueY2;

    public bool IsNetIncomeGrowing3Y =>
        NetIncomeY0 > 0 && NetIncomeY1 > 0 && NetIncomeY2 > 0 &&
        NetIncomeY0 > NetIncomeY1 && NetIncomeY1 > NetIncomeY2;
}