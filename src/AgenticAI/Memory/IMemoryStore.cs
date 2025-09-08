namespace AgenticAI.Memory;

public interface IMemoryStore
{
    Task AppendAsync(string sessionId, string role, string content, CancellationToken ct = default);
    IAsyncEnumerable<(string Role, string Content)> ReadAsync(string sessionId, int maxTurns = 20, CancellationToken ct = default);
}
