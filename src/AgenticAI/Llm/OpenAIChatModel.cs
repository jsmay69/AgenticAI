using AgenticAI.Llm.Interfaces;
using AgenticAI.Llm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgenticAI.Llm
{
    public sealed class OpenAIChatModel : IChatModel
    {
        private readonly HttpClient _http;
        public string Provider => "OpenAI";
        public string Model { get; }
        public OpenAIChatModel(HttpClient http, string model) { _http = http; Model = model; }

        public async Task<ChatResult> CompleteAsync(string system, IEnumerable<(string role, string content)> msgs, CancellationToken ct = default)
        {
            var payload = new
            {
                model = Model,
                messages = new[] { new { role = "system", content = system } }
                    .Concat(msgs.Select(m => new { m.role, m.content })),
                temperature = 0.2
            };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var res = await _http.PostAsJsonAsync("chat/completions", payload, ct);
            sw.Stop();
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            var text = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
            int? pt = json.TryGetProperty("usage", out var u) && u.TryGetProperty("prompt_tokens", out var v1) ? v1.GetInt32() : null;
            int? ctok = json.TryGetProperty("usage", out var u2) && u2.TryGetProperty("completion_tokens", out var v2) ? v2.GetInt32() : null;
            int? tot = json.TryGetProperty("usage", out var u3) && u3.TryGetProperty("total_tokens", out var v3) ? v3.GetInt32() : null;

            return new ChatResult(Provider, Model, text, pt, ctok, tot, sw.ElapsedMilliseconds);
        }
    }

}
