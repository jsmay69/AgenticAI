namespace AgenticAI.Memory;

public class NullMemoryStore : IMemoryStore
{
    public Task AppendAsync(string sessionId, string role, string content, CancellationToken ct = default) => Task.CompletedTask;

    public async IAsyncEnumerable<(string Role, string Content)> ReadAsync(string sessionId, int maxTurns = 20, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        yield break;
    }
}
