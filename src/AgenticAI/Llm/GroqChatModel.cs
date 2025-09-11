using AgenticAI.Core;
using AgenticAI.Llm.Interfaces;
using AgenticAI.Llm.Models;
using System;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using static AgenticAI.Llm.Models.LlmOptions;

namespace AgenticAI.Llm
{
    public sealed class GroqChatModel : IChatModel
    {
        private readonly HttpClient _http;
        private readonly GroqOptions _options;
        public string Provider => "Groq";
        public string Model { get; }
        public GroqChatModel(HttpClient http, GroqOptions options)
        {
            _http = http;
            _options = options;
            Model = _options.Model;
        }



        public async Task<ChatResult> CompleteAsync(string system, IEnumerable<ChatTurn> history, CancellationToken ct = default)
        {
            var messages = new List<Dictionary<string, string>>();
           
            if (!string.IsNullOrWhiteSpace(system))
                messages.Add(new() { ["role"] = "system", ["content"] = system });
            foreach (var h in history)
            {
                var role = h.Role;
                if (string.Equals(role, "tool", StringComparison.OrdinalIgnoreCase))
                    role = "user";
                messages.Add(new() { ["role"] = role, ["content"] = h.Content });
            }

            var payload = new
            {
                model = Model,
                temperature = 0.2,
                messages,
                max_completion_tokens = 4096,
                reasoning_effort = "default",
                stream = false,
            };
        
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            using var res = await _http.PostAsJsonAsync("chat/completions", payload, ct);
            sw.Stop();
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
       
            var text = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
            text = Regex.Replace(text ?? "", @"<think\b[^>]*>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
            int? pt = json.TryGetProperty("usage", out var u) && u.TryGetProperty("prompt_tokens", out var v1) ? v1.GetInt32() : null;
            int? ctok = json.TryGetProperty("usage", out var u2) && u2.TryGetProperty("completion_tokens", out var v2) ? v2.GetInt32() : null;
            int? tot = json.TryGetProperty("usage", out var u3) && u3.TryGetProperty("total_tokens", out var v3) ? v3.GetInt32() : null;

            return new ChatResult(Provider, Model, text, pt, ctok, tot, sw.ElapsedMilliseconds);
        }
    }

}
