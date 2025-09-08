using AgenticAI.Core;

namespace AgenticAI.Llm;

public interface ILLMClient
{
    Task<string> CompleteAsync(string systemPrompt, IEnumerable<ChatTurn> history, CancellationToken ct = default);
    Task<string> ModelNameAsync();
}
