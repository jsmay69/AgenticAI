using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgenticAI.Llm.Models
{
    public sealed record ChatResult(
        string Provider,
        string Model,
        string Text,
        int? PromptTokens,
        int? CompletionTokens,
        int? TotalTokens,
        long LatencyMs);

}
