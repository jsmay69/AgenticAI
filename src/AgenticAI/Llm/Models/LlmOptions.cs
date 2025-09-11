namespace AgenticAI.Llm.Models
{
    public sealed record LlmOptions
    {
        public int MaxSteps { get; set; } = 8;
        public string SystemPrompt { get; set; } = "You are a tool-using assistant.";
        public string DefaultProvider { get; init; } = "Groq";
        public OpenAIOptions OpenAI { get; init; } = new();
        public OllamaOptions Ollama { get; init; } = new();
        public GroqOptions Groq { get; init; } = new();
        public sealed record OpenAIOptions { public string ApiKey { get; init; } = ""; public string Model { get; init; } = "gpt-4o-mini"; public string BaseUrl { get; init; } = "https://api.openai.com/v1"; }
        public sealed record OllamaOptions { public string Host { get; init; } = "http://localhost:11434"; public string Model { get; init; } = "llama3.1:8b"; }
        public sealed record GroqOptions { public string ApiKey { get; init; } = ""; public string Model { get; init; } = "llama-3.1-8b-instant"; public string BaseUrl { get; init; } = "https://api.groq.com/openai/v1"; }
    }
}
