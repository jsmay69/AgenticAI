using AgenticAI.Core;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgenticAI.Llm;

public class GroqClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public GroqClient(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient();
        _apiKey = "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"; // Environment.GetEnvironmentVariable(config.GetSection("Groq")["ApiKey"] ?? "OPENAI_API_KEY") ?? "";
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY environment variable not set.");
        _baseUrl = config.GetSection("Groq")["BaseUrl"] ?? "https://api.groq.com/openai/v1";
        _model = config.GetSection("Groq")["Model"] ?? "qwen/qwen3-32b";
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
            messages,
            max_completion_tokens = 4096,
            reasoning_effort = "default",
            stream = false,
        };

        using var resp = await _http.PostAsJsonAsync(url, payload, cancellationToken: ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        content = Regex.Replace(content ?? "", @"<think\b[^>]*>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

        return content ?? "";
    }
}
