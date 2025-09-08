using System.Net.Http.Json;
using System.Text.Json;
using AgenticAI.Core;
using Microsoft.Extensions.Configuration;

namespace AgenticAI.Llm;

public class OllamaClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;

    public OllamaClient(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient();
        _baseUrl = config.GetSection("Ollama")["BaseUrl"] ?? "http://localhost:11434";
        _model = config.GetSection("Agent")["Model"] ?? config.GetSection("Ollama")["Model"] ?? "llama3.1:8b-instruct-q8_0";
    }

    public Task<string> ModelNameAsync() => Task.FromResult(_model);

    public async Task<string> CompleteAsync(string systemPrompt, IEnumerable<ChatTurn> history, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/chat";
        var messages = new List<Dictionary<string, string>>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new() { ["role"] = "system", ["content"] = systemPrompt });

        foreach (var h in history)
            messages.Add(new() { ["role"] = h.Role, ["content"] = h.Content });

        var payload = new
        {
            model = _model,
            messages,
            stream = false,
            options = new { temperature = 0.2 }
        };

        using var resp = await _http.PostAsJsonAsync(url, payload, cancellationToken: ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString();
        return content ?? "";
    }
}
