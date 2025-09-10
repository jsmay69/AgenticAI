using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgenticAI.Llm.Interfaces
{
    public enum LlmProvider { OpenAI, Ollama, Groq }

    public interface IChatModelSelector
    {
        IChatModel Select(LlmProvider? overrideProvider = null);
    }
}
