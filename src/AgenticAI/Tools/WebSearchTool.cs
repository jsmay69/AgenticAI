using System.Text.Json;
using System.Linq;
using AgenticAI.Core;

namespace AgenticAI.Tools;

public class WebSearchTool : ITool
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public WebSearchTool(IHttpClientFactory factory, string apiKey)
    {
        _http = factory.CreateClient();
        _apiKey = apiKey;
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
        
        var temp = args.TryGetValue("numResults", out var n) ? n : null;
        if (temp != null)
        {
            if (temp is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var intVal))
            {
                if (intVal < 1 || intVal > 10) return new { error = "numResults must be between 1 and 10" };
            }
            else if (temp is int intVal2)
            {
                if (intVal2 < 1 || intVal2 > 10) return new { error = "numResults must be between 1 and 10" };
            }
            else
            {
                return new { error = "numResults must be an integer" };
            }
        }
        var numResultsstring = Convert.ToString(temp);
        var numResults = !string.IsNullOrWhiteSpace(numResultsstring) && int.TryParse(numResultsstring, out var parsed) ? parsed : 3;   
       
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
