using AgenticAI.Llm.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticAI.Llm.DI
{

    public static class LlmDI
    {
        public static IServiceCollection AddLlm(this IServiceCollection s, IConfiguration cfg)
        {
            var opts = new LlmOptions(); cfg.GetSection("LLM").Bind(opts);
            s.AddSingleton(opts);

            s.AddHttpClient("OpenAI", c => {
                c.BaseAddress = new Uri(opts.OpenAI.BaseUrl);
                var key = ResolveKey(opts.OpenAI.ApiKey);
                if (!string.IsNullOrWhiteSpace(key))
                    c.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            });
            s.AddHttpClient("Ollama", c => c.BaseAddress = new Uri(opts.Ollama.Host));

            s.AddHttpClient("Groq", c => {
                c.BaseAddress = new Uri(opts.Groq.BaseUrl);
                var key = ResolveKey(opts.Groq.ApiKey);
                if (!string.IsNullOrWhiteSpace(key))
                    c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            });

            s.AddKeyedTransient<IChatModel>("OpenAI", sp =>
                new OpenAIChatModel(sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenAI"), opts.OpenAI.Model));
            s.AddKeyedTransient<IChatModel>("Ollama", sp =>
                new OllamaChatModel(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Ollama"), opts.Ollama.Model));
            s.AddKeyedTransient<IChatModel>("Groq", sp =>
                new GroqChatModel(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Groq"), opts.Groq.Model));

            s.AddSingleton<IChatModelSelector>(sp => new Selector(
                sp.GetRequiredKeyedService<IChatModel>("OpenAI"),
                sp.GetRequiredKeyedService<IChatModel>("Ollama"),
                sp.GetRequiredKeyedService<IChatModel>("Groq"),
                Enum.Parse<LlmProvider>(opts.DefaultProvider, true)));

            return s;
        }

        private static string ResolveKey(string v) => v.StartsWith("env:", StringComparison.OrdinalIgnoreCase)
            ? Environment.GetEnvironmentVariable(v["env:".Length..]) ?? ""
            : v;

        private sealed class Selector(IChatModel openai, IChatModel ollama, IChatModel groq, LlmProvider @default) : IChatModelSelector
        {
            public IChatModel Select(LlmProvider? overrideProvider = null)
            {
                var p = overrideProvider ?? @default;
                return p switch
                {
                    LlmProvider.OpenAI => openai,
                    LlmProvider.Ollama => ollama,
                    LlmProvider.Groq => groq,
                    _ => openai
                };
            }
        }
    }

}
