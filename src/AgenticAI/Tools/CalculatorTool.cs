using System.Text.Json;
using AgenticAI.Core;

namespace AgenticAI.Tools;

public class CalculatorTool : ITool
{
    public string Name => "calculator";
    public string Description => "Evaluates a basic arithmetic expression. Use for math.";
    public JsonElement Schema => JsonDocument.Parse(@"{""type"":""object"",""properties"":{""expr"":{""type"":""string""}},""required"":[""expr""],""additionalProperties"":false}").RootElement;

    public Task<object?> ExecuteAsync(Dictionary<string, object?> args, ToolContext ctx, CancellationToken ct = default)
    {
        var expr = args.TryGetValue("expr", out var v) ? Convert.ToString(v) ?? "" : "";
        if (string.IsNullOrWhiteSpace(expr)) return Task.FromResult<object?>(new { error = "expr is required" });

        try
        {
            var dt = new System.Data.DataTable();
            var value = dt.Compute(expr, null);
            return Task.FromResult<object?>(new { result = value });
        }
        catch
        {
            return Task.FromResult<object?>(new { error = "failed to evaluate expression" });
        }
    }
}
