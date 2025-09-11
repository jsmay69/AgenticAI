using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;


namespace AgenticAI.Tools;

public class ChromaRagTool : ITool
{
    private readonly HttpClient _http;
    private readonly string _mcpUrl;

    public ChromaRagTool(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient();
        _mcpUrl = config.GetSection("Chroma")["McpUrl"] ?? "http://localhost:8000/mcp";
    }

    public string Name => "chroma_rag";
    public string Description => "Queries a Chroma database (via MCP) for relevant documents.";
    public JsonElement Schema => JsonDocument.Parse(@"
    {
      \"type\": \"object\",
      \"properties\": {
        \"collection\": { \"type\": \"string\", \"description\": \"Chroma collection name\" },
        \"query\": { \"type\": \"string\", \"description\": \"Natural language query\" },
        \"nResults\": { \"type\": \"integer\", \"default\": 3 }
      },
      \"required\": [\"collection\", \"query\"],
      \"additionalProperties\": false
    }").RootElement;

    public async Task<object?> ExecuteAsync(Dictionary<string, object?> args, ToolContext ctx, CancellationToken ct = default)
    {
        var collection = Convert.ToString(args["collection"]) ?? string.Empty;
        var query = Convert.ToString(args["query"]) ?? string.Empty;

        var nResults = 3;
        if (args.TryGetValue("nResults", out var nObj) && nObj != null &&
            int.TryParse(Convert.ToString(nObj), out var parsed))
        {
            nResults = parsed;
        }

        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = "chroma.query",
            @params = new
            {
                collection,
                query_texts = new[] { query },
                n_results = nResults
            }
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(_mcpUrl, content, ct);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("result", out var result))
        {
            if (result.TryGetProperty("documents", out var docs) &&
                docs.ValueKind == JsonValueKind.Array && docs.GetArrayLength() > 0)
            {
                var first = docs[0];
                if (first.ValueKind == JsonValueKind.Array)
                {
                    return first.EnumerateArray().Select(e => e.GetString()).ToArray();
                }
            }

            return result;
        }

        return doc.RootElement;
    }
}
