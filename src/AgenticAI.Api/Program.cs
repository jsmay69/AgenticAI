using AgenticAI.Core;
using AgenticAI.Llm;
using AgenticAI.Llm.DI;
using AgenticAI.Llm.Interfaces;
using AgenticAI.Memory;
using AgenticAI.Tools;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var services = builder.Services;

services.Configure<AgentOptions>(config.GetSection("Agent"));

// OpenTelemetry Console exporter
services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgentAPI"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// LLM DI
services.AddLlm(config);

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
else if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
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
services.AddSingleton<ITool, FileWriteTool>(sp =>
{
    var ws = config.GetSection("Tools")["Workspace"] ?? "workspace";
    Directory.CreateDirectory(ws);
    return new FileWriteTool(ws);
});

services.AddSingleton<IAgent, ReactiveAgent>();

services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
