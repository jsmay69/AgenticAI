using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgenticAI.Core;
using AgenticAI.Llm.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AgenticAI.Llm;

public class OpenAIClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public OpenAIClient(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient();
        _apiKey = Environment.GetEnvironmentVariable(config.GetSection("OpenAI")["ApiKeyEnv"] ?? "OPENAI_API_KEY") ?? "";
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY environment variable not set.");
        _baseUrl = config.GetSection("OpenAI")["BaseUrl"] ?? "https://api.openai.com/v1";
        _model = config.GetSection("Agent")["Model"] ?? config.GetSection("OpenAI")["Model"] ?? "gpt-4o-mini";
    }

    public Task<string> ModelNameAsync() => Task.FromResult(_model);

    public async Task<string> CompleteAsync(string systemPrompt, IEnumerable<ChatTurn> history, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/chat/completions";
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var messages = new List<Dictionary<string, string>>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new() { ["role"] = "system", ["content"] = systemPrompt });
        foreach (var h in history)
            messages.Add(new() { ["role"] = h.Role, ["content"] = h.Content });

        var payload = new
        {
            model = _model,
            temperature = 0.2,
            messages
        };

        using var resp = await _http.PostAsJsonAsync(url, payload, cancellationToken: ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? "";
    }
}
