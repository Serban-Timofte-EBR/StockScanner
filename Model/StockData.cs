namespace StockScanner.Model;

public class StockData
{
    public string Symbol { get; set; } = "";
    public string ShortName { get; set; } = "";
    public double MarketCap { get; set; }
    public double Price { get; set; }

    public double ForwardPE { get; set; }
    public double PegRatio { get; set; }
    public double EvToEbitda { get; set; }

    // y0 = latest year, y1 = previous year, y2 = 2 years ago (e.g., 2024/2023/2022 depending on availability)
    public long RevenueY0 { get; set; }
    public long RevenueY1 { get; set; }
    public long RevenueY2 { get; set; }

    public long NetIncomeY0 { get; set; }
    public long NetIncomeY1 { get; set; }
    public long NetIncomeY2 { get; set; }

    public double EpsGrowth5Y { get; set; } // 0.10 = 10%

    // Quality & Risk
    public double ReturnOnEquity { get; set; }
    public double TotalCash { get; set; }
    public double TotalDebt { get; set; }
    public double Ebitda { get; set; }
    public double CurrentRatio { get; set; }
    public double FreeCashFlow { get; set; }

    public double NetDebt => TotalDebt - TotalCash;

    // IMPORTANT: invalid EBITDA => Infinity (not 0, which would look "good")
    public double NetDebtToEbitda => Ebitda > 0 ? NetDebt / Ebitda : double.PositiveInfinity;

    public bool IsRevenueGrowing3Y => RevenueY0 > 0 && RevenueY1 > 0 && RevenueY2 > 0
                                      && RevenueY0 > RevenueY1 && RevenueY1 > RevenueY2;

    public bool IsNetIncomeGrowing3Y => NetIncomeY0 > 0 && NetIncomeY1 > 0 && NetIncomeY2 > 0
                                        && NetIncomeY0 > NetIncomeY1 && NetIncomeY1 > NetIncomeY2;

    public override string ToString()
        => $"{Symbol} | Cap: {MarketCap:N0} | PEG: {PegRatio:F2} | Score: [PENDING]";
}