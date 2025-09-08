namespace AgenticAI.Core;

public interface IAgent
{
    Task<AgentResult> RunAsync(AgentTask task, CancellationToken ct = default);
}
