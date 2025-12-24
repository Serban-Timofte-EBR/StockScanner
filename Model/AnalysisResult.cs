namespace StockScanner.Model;

using System.Collections.Generic;

public class AnalysisResult
{
    public StockData Stock { get; set; }
    public int Score { get; set; }
    public string Recommendation { get; set; }
    public List<string> Reasons { get; set; } = new List<string>();
    public bool IsRejected { get; set; }
}