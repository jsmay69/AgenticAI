using AgenticAI.Core;
using AgenticAI.Llm.Interfaces;
using AgenticAI.Llm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgenticAI.Llm
{
    public sealed class GroqChatModel : IChatModel
    {
        private readonly HttpClient _http;
        public string Provider => "Groq";
        public string Model { get; }
        public GroqChatModel(HttpClient http, string model) { _http = http; Model = model; }

        public async Task<ChatResult> CompleteAsync(string system, IEnumerable<ChatTurn> history, CancellationToken ct = default)
        {


            var messages = new List<Dictionary<string, string>>();
           
            if (!string.IsNullOrWhiteSpace(system))
                messages.Add(new() { ["role"] = "system", ["content"] = system });
            foreach (var h in history)
                messages.Add(new() { ["role"] = h.Role, ["content"] = h.Content });

            var payload = new
            {
                model = Model,
                temperature = 0.2,
                messages,
                max_completion_tokens = 4096,
                reasoning_effort = "default",
                stream = false,
            };
            //var payload = new
            //{
            //    model = Model,
            //    messages = new[] { new { role = "system", content = system } }
            //        .Concat(history.Select(m => new { m.Role, m.Content })),
            //    temperature = 0.0
            //};
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var res = await _http.PostAsJsonAsync("chat/completions", payload, ct);
            sw.Stop();
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);

          
            //using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            //var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            //content = Regex.Replace(content ?? "", @"<think\b[^>]*>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

            //return content ?? "";

            var text = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
            int? pt = json.TryGetProperty("usage", out var u) && u.TryGetProperty("prompt_tokens", out var v1) ? v1.GetInt32() : null;
            int? ctok = json.TryGetProperty("usage", out var u2) && u2.TryGetProperty("completion_tokens", out var v2) ? v2.GetInt32() : null;
            int? tot = json.TryGetProperty("usage", out var u3) && u3.TryGetProperty("total_tokens", out var v3) ? v3.GetInt32() : null;

            return new ChatResult(Provider, Model, text, pt, ctok, tot, sw.ElapsedMilliseconds);
        }
    }

}
