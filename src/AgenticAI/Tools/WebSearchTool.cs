using System.Text.Json;
using System.Linq;
using AgenticAI.Core;

namespace AgenticAI.Tools;

public class WebSearchTool : ITool
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public WebSearchTool(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
        // Example: using SerpAPI (https://serpapi.com/)
        _apiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY") 
                  ?? throw new InvalidOperationException("SERPAPI_KEY not set");
        _baseUrl = "https://serpapi.com/search";
    }

    public string Name => "web_search";
    public string Description => "Searches the web for information using SerpAPI.";
    public JsonElement Schema => JsonDocument.Parse(@"
    {
      ""type"": ""object"",
      ""properties"": {
        ""query"": { ""type"": ""string"", ""description"": ""The search query text"" },
        ""numResults"": { ""type"": ""integer"", ""default"": 3 }
      },
      ""required"": [""query""],
      ""additionalProperties"": false
    }").RootElement;

    private record SearchResult(string? Title, string? Link);

    public async Task<object?> ExecuteAsync(Dictionary<string, object?> args, ToolContext ctx, CancellationToken ct = default)
    {
        var query = Convert.ToString(args["query"]) ?? "";
        var numResults = args.TryGetValue("numResults", out var n) ? Convert.ToInt32(n) : 3;

        var url = $"{_baseUrl}?engine=google&q={Uri.EscapeDataString(query)}&num={numResults}&api_key={_apiKey}";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var results = new List<SearchResult>();

        if (doc.RootElement.TryGetProperty("organic_results", out var organic) && organic.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in organic.EnumerateArray().Take(numResults))
            {
                string? title = null;
                string? link = null;
                if (r.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String) title = t.GetString();
                if (r.TryGetProperty("link", out var l) && l.ValueKind == JsonValueKind.String) link = l.GetString();
                results.Add(new SearchResult(title, link));
            }
        }

        return results;
    }
}
