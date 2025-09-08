using System.Text.Json;
using AgenticAI.Core;

namespace AgenticAI.Tools;

public class TimeTool : ITool
{
    public string Name => "time_now";
    public string Description => "Returns the current UTC and local time for the system.";
    public JsonElement Schema => JsonDocument.Parse(@"{""type"":""object"",""properties"":{},""additionalProperties"":false}").RootElement;

    public Task<object?> ExecuteAsync(Dictionary<string, object?> args, ToolContext ctx, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult<object?>(new {
            utc = now,
            local = now.ToLocalTime()
        });
    }
}
