using System.Collections.Concurrent;
using System.Threading;

namespace AgenticAI.Memory;

public class FileMemoryStore : IMemoryStore
{
    private readonly string _dir;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public FileMemoryStore(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(_dir);
    }

    public async Task AppendAsync(string sessionId, string role, string content, CancellationToken ct = default)
    {
        var path = Path.Combine(_dir, $"{San(sessionId)}.log");
        var gate = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        var line = $"{DateTimeOffset.UtcNow:o}\t{role}\t{content}\n";
        await gate.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(path, line, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async IAsyncEnumerable<(string Role, string Content)> ReadAsync(string sessionId, int maxTurns = 20, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var path = Path.Combine(_dir, $"{San(sessionId)}.log");
        if (!File.Exists(path)) yield break;

        var lines = File.ReadLines(path).Reverse().Take(maxTurns).Reverse();
        foreach (var l in lines)
        {
            var parts = l.Split('\t');
            if (parts.Length >= 3)
                yield return (parts[1], parts[2]);
            await Task.Yield();
        }
    }

    private static string San(string s) => string.Concat(s.Where(char.IsLetterOrDigit));
}
