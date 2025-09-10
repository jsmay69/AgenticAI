using AgenticAI.Llm.Interfaces;
using AgenticAI.Llm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace AgenticAI.Llm
{
    public sealed class OllamaChatModel : IChatModel
    {
        private readonly HttpClient _http;
        public string Provider => "Ollama";
        public string Model { get; }
        public OllamaChatModel(HttpClient http, string model) { _http = http; Model = model; }

        public async Task<ChatResult> CompleteAsync(string system, IEnumerable<(string role, string content)> msgs, CancellationToken ct = default)
        {
            // Prefer /api/chat with JSON mode for tool-friendly strict outputs
            var messages = new List<object> {
            new { role = "system", content = system }
        };
            messages.AddRange(msgs.Select(m => new { m.role, m.content }));

            var req = new
            {
                model = Model,
                messages,
                stream = false,
                format = "json", // enforce valid JSON outputs
                options = new { temperature = 0, num_predict = 256 }
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var res = await _http.PostAsJsonAsync("/api/chat", req, ct);
            sw.Stop();
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);

            string text = "";
            if (json.TryGetProperty("message", out var m) && m.TryGetProperty("content", out var mc))
                text = mc.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                // Fallback: try /api/generate once (also JSON format) to recover from empty content
                var prompt = string.Join("\n", new[] { $"[SYSTEM]\n{system}\n" }
                    .Concat(msgs.Select(m2 => $"[{m2.role.ToUpper()}]\n{m2.content}\n")));
                var genReq = new { model = Model, prompt, stream = false, format = "json", options = new { temperature = 0, num_predict = 256 } };
                using var res2 = await _http.PostAsJsonAsync("/api/generate", genReq, ct);
                res2.EnsureSuccessStatusCode();
                var json2 = await res2.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
                text = json2.TryGetProperty("response", out var r) ? r.GetString() ?? "" : "";
                int? pt2 = json2.TryGetProperty("prompt_eval_count", out var p2) ? p2.GetInt32() : null;
                int? ctok2 = json2.TryGetProperty("eval_count", out var c2) ? c2.GetInt32() : null;
                int? tot2 = pt2.HasValue && ctok2.HasValue ? pt2 + ctok2 : null;
                return new ChatResult(Provider, Model, text, pt2, ctok2, tot2, sw.ElapsedMilliseconds);
            }

            // token counts from /api/chat response (Ollama includes these at root when available)
            int? pt = json.TryGetProperty("prompt_eval_count", out var p) ? p.GetInt32() : null;
            int? ctok = json.TryGetProperty("eval_count", out var c) ? c.GetInt32() : null;
            int? tot = pt.HasValue && ctok.HasValue ? pt + ctok : null;

            return new ChatResult(Provider, Model, text, pt, ctok, tot, sw.ElapsedMilliseconds);
        }
    }


}
