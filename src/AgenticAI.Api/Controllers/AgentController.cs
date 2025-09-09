using AgenticAI.Core;
using Microsoft.AspNetCore.Mvc;

namespace AgenticAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgent _agent;

    public AgentController(IAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("run")]
    public async Task<ActionResult<AgentResult>> Run([FromBody] AgentRequest request)
    {
        var result = await _agent.RunAsync(new AgentTask { Instruction = request.Instruction });
        return Ok(result);
    }
}

public record AgentRequest(string Instruction);
