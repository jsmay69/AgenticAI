using System.Text.Json.Serialization;

namespace AgenticAI.Core;

public record AgentTask
{
    public string Instruction { get; init; } = "";
    public Dictionary<string, object?>? Context { get; init; }
    public string? SessionId { get; init; }
}

public record AgentResult(string Output, int Steps, List<ToolInvocation> ToolInvocations);

public record ChatTurn(string Role, string Content);

public record ToolInvocation(string ToolName, string ArgumentsJson, string? ResultJson);

public record ToolCall
{
    [JsonPropertyName("tool")]
    public string Tool { get; init; } = "";

    [JsonPropertyName("arguments")]
    public Dictionary<string, object?> Arguments { get; init; } = new();
}
