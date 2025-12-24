using System.Net;

namespace StockScanner.Clients;

public class YahooSession
{
    private string _crumb;
    private readonly HttpClient _client;
    private readonly CookieContainer _cookieContainer;

    public YahooSession()
    {
        _cookieContainer = new CookieContainer();

        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true
        };
        
        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task IniatilizeAsync()
    {
        if (!string.IsNullOrEmpty(_crumb)) return;
        
        Console.WriteLine("[YahooSession] Authenticating with Yahoo");

        try
        {
            var response = await _client.GetAsync("https://fc.yahoo.com");
            var crumbResponse = await _client.GetAsync("https://query2.finance.yahoo.com/v1/test/getcrumb");
                
            if (crumbResponse.IsSuccessStatusCode)
            {
                _crumb = await crumbResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[YahooSession] Crumb obtained: {_crumb}");
            }
            else
            {
                throw new Exception($"[YahooSession] Failed to get crumb. Status: {crumbResponse.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[YahooSession] Authentication Failed: {e.Message}");
            throw;
        }
    }
    
    public HttpClient GetClient() => _client;
    public string GetCrumb() => _crumb;
}