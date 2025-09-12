using System.Text.Json;

namespace AgenticAI.Tools;

public class PdfDownloadTool : ITool
{
    private readonly HttpClient _http;
    private readonly string _workspace;

    public PdfDownloadTool(IHttpClientFactory factory, string workspace)
    {
        _http = factory.CreateClient();
        _workspace = workspace;
    }

    public string Name => "pdf_download";
    public string Description => "Downloads a PDF from a URL to a file inside the configured workspace.";
    public JsonElement Schema => JsonDocument.Parse(@"{
  \"type\": \"object\",
  \"properties\": {
    \"url\": { \"type\": \"string\", \"description\": \"The URL of the PDF to download\" },
    \"relativePath\": { \"type\": \"string\", \"description\": \"Optional relative path to save the PDF under the workspace\" }
  },
  \"required\": [\"url\"],
  \"additionalProperties\": false
}").RootElement;

    public async Task<object?> ExecuteAsync(Dictionary<string, object?> args, ToolContext ctx, CancellationToken ct = default)
    {
        var url = Convert.ToString(args["url"]) ?? "";
        if (string.IsNullOrWhiteSpace(url)) return new { error = "url is required" };

        var relative = Convert.ToString(args.GetValueOrDefault("relativePath"));
        if (string.IsNullOrWhiteSpace(relative))
        {
            var uri = new Uri(url);
            relative = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(relative)) relative = "download.pdf";
        }

        var wsFull = Path.GetFullPath(_workspace);
        var full = Path.GetFullPath(Path.Combine(wsFull, relative));
        if (!full.StartsWith(wsFull, StringComparison.Ordinal))
            return new { error = "path escapes workspace" };

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return new { error = $"HTTP {resp.StatusCode}" };

        var contentType = resp.Content.Headers.ContentType?.MediaType;
        if (contentType != null && !contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            return new { error = "URL does not point to a PDF" };

        await using (var fs = File.Create(full))
        {
            await resp.Content.CopyToAsync(fs, ct);
        }

        var rel = Path.GetRelativePath(wsFull, full).Replace("\\", "/");
        var size = new FileInfo(full).Length;
        return new { path = rel, size };
    }
}

