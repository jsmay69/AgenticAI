using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgenticAI.Memory;
using AgenticAI.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgenticAI.Llm;

namespace AgenticAI.Core;

public class ReactiveAgent : IAgent
{
    private readonly ILLMClient _llm;
    private readonly ToolRegistry _tools;
    private readonly IMemoryStore _memory;
    private readonly AgentOptions _options;
    private readonly ILogger<ReactiveAgent> _log;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerOptions.Default)
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string ToolCallInstruction = @"You can either ANSWER or CALL_TOOL.
To call a tool, reply with ONLY a single JSON object:
{
  ""decision"":""CALL_TOOL"",
  ""tool"": ""<tool_name>"",
  ""arguments"": { ...object per tool schema... }
}
To answer normally, reply with:
{ ""decision"":""ANSWER"", ""final"": ""<message>"" }
No other text.
Available tools are listed below as JSON with name, description, and schema."";

    public ReactiveAgent(ILLMClient llm, ToolRegistry tools, IMemoryStore memory, IOptions<AgentOptions> options, ILogger<ReactiveAgent> log)
    {
        _llm = llm;
        _tools = tools;
        _memory = memory;
        _options = options.Value;
        _log = log;
    }

    public async Task<AgentResult> RunAsync(AgentTask task, CancellationToken ct = default)
    {
        var session = task.SessionId ?? "default";
        var turns = new List<ChatTurn>();
        await foreach (var (role, content) in _memory.ReadAsync(session, 20, ct))
            turns.Add(new ChatTurn(role, content));

        turns.Add(new ChatTurn("user", task.Instruction));

        var toolList = JsonSerializer.Serialize(_tools.ListTools(), _json);

        var steps = 0;
        var invocations = new List<ToolInvocation>();
        string? final = null;

        while (steps < _options.MaxSteps && final == null)
        {
            steps++;
            var prompt = new StringBuilder();
            prompt.AppendLine("You are an agent that may use tools.");
            prompt.AppendLine(ToolCallInstruction);
            prompt.AppendLine("TOOLS:");
            prompt.AppendLine(toolList);
            prompt.AppendLine("If you already have enough info, ANSWER.");
            prompt.AppendLine("Otherwise, CALL_TOOL.");

            var history = new List<ChatTurn>(turns)
            {
                new ChatTurn("system", prompt.ToString())
            };

            var modelOut = await _llm.CompleteAsync(_options.SystemPrompt, history, ct);
            _log.LogInformation("Model raw: {raw}", modelOut);

            try
            {
                using var doc = JsonDocument.Parse(modelOut);
                var root = doc.RootElement;
                var decision = root.GetProperty("decision").GetString();
                if (string.Equals(decision, "ANSWER", StringComparison.OrdinalIgnoreCase))
                {
                    final = root.GetProperty("final").GetString() ?? "";
                    break;
                }
                if (!string.Equals(decision, "CALL_TOOL", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Invalid decision");

                var tool = root.GetProperty("tool").GetString() ?? "";
                var args = root.GetProperty("arguments").Deserialize<Dictionary<string, object?>>() ?? new();

                var impl = _tools.Get(tool);
                if (impl is null)
                {
                    final = $"Requested unknown tool '{tool}'.";
                    break;
                }

                var result = await impl.ExecuteAsync(args, new ToolContext(_memory, this as IServiceProvider ?? null!), ct);
                var resultJson = JsonSerializer.Serialize(result, _json);

                invocations.Add(new ToolInvocation(tool, JsonSerializer.Serialize(args, _json), resultJson));
                turns.Add(new ChatTurn("tool", $"tool:{tool}\nargs:{JsonSerializer.Serialize(args, _json)}\nresult:{resultJson}"));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to parse or execute tool JSON. Falling back to plain answer.");
                final = modelOut.Trim();
                break;
            }
        }

        final ??= "No answer generated.";
        await _memory.AppendAsync(session, "user", task.Instruction, ct);
        await _memory.AppendAsync(session, "assistant", final, ct);

        return new AgentResult(final, steps, invocations);
    }
}
