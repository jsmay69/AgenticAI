using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AgenticAI.Core;
using AgenticAI.Llm;
using AgenticAI.Memory;
using AgenticAI.Tools;

namespace AgenticAI;

public class Program
{
    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                var config = ctx.Configuration;

                services.Configure<AgentOptions>(config.GetSection("Agent"));

                var memoryType = config.GetSection("Memory")["Type"] ?? "File";
                if (string.Equals(memoryType, "File", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = config.GetSection("Memory")["Directory"] ?? "data/memory";
                    services.AddSingleton<IMemoryStore>(new FileMemoryStore(dir));
                }
                else
                {
                    services.AddSingleton<IMemoryStore, NullMemoryStore>();
                }

                services.AddHttpClient();

                var provider = config.GetSection("Agent")["Provider"] ?? "Ollama";
                if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddSingleton<ILLMClient, OpenAIClient>();
                }
                else if(string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddSingleton<ILLMClient, OllamaClient>();
                }
                else
                {
                    services.AddSingleton<ILLMClient, GroqClient>();
                }

                services.AddSingleton<ToolRegistry>();
                services.AddSingleton<ITool, TimeTool>();
                services.AddSingleton<ITool, CalculatorTool>();
                var serpApiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY");
                if (!string.IsNullOrEmpty(serpApiKey))
                {
                    services.AddSingleton<ITool>(sp => new WebSearchTool(sp.GetRequiredService<IHttpClientFactory>(), serpApiKey));
                }
                else
                {
                    Console.WriteLine("SERPAPI_KEY not set. Web search tool disabled.");
                }
                services.AddSingleton<ITool, FileWriteTool>(sp =>
                {
                    var ws = config.GetSection("Tools")["Workspace"] ?? "workspace";
                    Directory.CreateDirectory(ws);
                    return new FileWriteTool(ws);
                });

                services.AddSingleton<IAgent, ReactiveAgent>();

                services.AddLogging(b => b.AddConsole());
            })
            .Build();

        var agent = host.Services.GetRequiredService<IAgent>();

        Console.WriteLine("AgenticAI ready. Type your task. Ctrl+C to exit.");
        string? input;
        while ((input = Console.ReadLine()) != null)
        {
            var result = await agent.RunAsync(new AgentTask
            {
                Instruction = input
            });

            Console.WriteLine("\n=== Agent Result ===");
            Console.WriteLine(result.Output);
            Console.WriteLine("====================\n");
        }
    }
}
