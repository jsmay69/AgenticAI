using AgenticAI.Llm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgenticAI.Llm.Interfaces
{
    public interface IChatModel
    {
        string Provider { get; }
        string Model { get; }
        System.Threading.Tasks.Task<ChatResult> CompleteAsync(
            string system, System.Collections.Generic.IEnumerable<(string role, string content)> msgs,
            System.Threading.CancellationToken ct = default);
    }
}
