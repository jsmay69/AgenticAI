namespace AgenticAI.Core;

public class AgentOptions
{
    public string Provider { get; set; } = "Ollama";
    public string Model { get; set; } = "llama3.1:8b-instruct-q8_0";
    public int MaxSteps { get; set; } = 8;
    public string SystemPrompt { get; set; } = "You are a tool-using assistant.";
}
