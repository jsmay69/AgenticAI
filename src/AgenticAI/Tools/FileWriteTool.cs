using System.Text.Json;
using AgenticAI.Core;

namespace AgenticAI.Tools;

public class FileWriteTool : ITool
{
    private readonly string _workspace;
    public FileWriteTool(string workspace) { _workspace = workspace; }

    public string Name => "file_write";
    public string Description => "Writes text content to a file within the configured workspace. Returns the relative path.";
    public JsonElement Schema => JsonDocument.Parse(@"{""type"":""object"",""properties"":{""relativePath"":{""type"":""string""},""content"":{""type"":""string""}},""required"":[""relativePath"",""content""],""additionalProperties"":false}").RootElement;

    public async Task<object?> ExecuteAsync(Dictionary<string, object?> args, ToolContext ctx, CancellationToken ct = default)
    {
        var relative = Convert.ToString(args["relativePath"]) ?? "output.txt";
        var content = Convert.ToString(args["content"]) ?? "";

        var wsFull = Path.GetFullPath(_workspace);
        var full = Path.GetFullPath(Path.Combine(wsFull, relative));
        if (!full.StartsWith(wsFull, StringComparison.Ordinal))
            return new { error = "path escapes workspace" };

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllTextAsync(full, content, ct);
        var rel = Path.GetRelativePath(wsFull, full);
        return new { path = rel.Replace("\\", "/"), size = content.Length };
    }
}
