using AgenticAI.Llm.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticAI.Llm.DI
{

    public static class LlmDI
    {
        public static IServiceCollection AddLlm(this IServiceCollection serviceCollection, IConfiguration config)
        {
            var opts = new LlmOptions(); config.GetSection("LLM").Bind(opts);
            serviceCollection.AddSingleton(opts);

            serviceCollection.AddHttpClient("OpenAI", c => {
                c.BaseAddress = new Uri(opts.OpenAI.BaseUrl);
                var key = ResolveKey(opts.OpenAI.ApiKey);
                if (!string.IsNullOrWhiteSpace(key))
                    c.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            });
            serviceCollection.AddHttpClient("Ollama", c => c.BaseAddress = new Uri(opts.Ollama.Host));

            serviceCollection.AddHttpClient("Groq", c => {
                c.BaseAddress = new Uri(opts.Groq.BaseUrl);
                var key = ResolveKey(opts.Groq.ApiKey);
                if (!string.IsNullOrWhiteSpace(key))
                    c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            });

            serviceCollection.AddKeyedTransient<IChatModel>("OpenAI", (sp, _) =>
                new OpenAIChatModel(sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenAI"), opts.OpenAI.Model));
            serviceCollection.AddKeyedTransient<IChatModel>("Ollama", (sp, _) =>
                new OllamaChatModel(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Ollama"), opts.Ollama.Model));
            serviceCollection.AddKeyedTransient<IChatModel>("Groq", (sp, _) =>
                new GroqChatModel(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Groq"), opts.Groq.Model));

            serviceCollection.AddSingleton<IChatModelSelector>(sp => new Selector(
                sp.GetRequiredKeyedService<IChatModel>("OpenAI"),
                sp.GetRequiredKeyedService<IChatModel>("Ollama"),
                sp.GetRequiredKeyedService<IChatModel>("Groq"),
                Enum.Parse<LlmProvider>(opts.DefaultProvider, true)));

            return serviceCollection;
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
