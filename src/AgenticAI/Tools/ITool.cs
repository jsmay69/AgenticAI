using System.Text.Json;
using AgenticAI.Memory;

namespace AgenticAI.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonElement Schema { get; } // JSON Schema of arguments
    Task<object?> ExecuteAsync(Dictionary<string, object?> args, ToolContext ctx, CancellationToken ct = default);
}

public record ToolContext(IMemoryStore Memory, IServiceProvider Services);

public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        foreach (var t in tools) _tools[t.Name] = t;
    }

    public ITool? Get(string name) => _tools.TryGetValue(name, out var t) ? t : null;

    public IEnumerable<object> ListTools() => _tools.Values.Select(t => new {
        name = t.Name, description = t.Description, schema = t.Schema
    });
}
