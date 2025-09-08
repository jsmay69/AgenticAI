# AgenticAI (.NET 8, C#)

Production-ready agentic AI scaffold with pluggable LLMs (OpenAI, Ollama) and a standardized tool system.

## Features
- Providers: **OpenAI** (Chat Completions), **Ollama** (local `/api/chat`)
- Simple ReAct loop with JSON tool-call protocol
- **Tools** included:
  - `time_now`: returns current UTC and local time
  - `calculator`: evaluates a math expression
  - `file_write`: writes text to a file inside a sandboxed workspace
  - `web_search`: searches the web using [SerpAPI](https://serpapi.com/) (requires API key)
- File-backed conversational memory
- Configurable via `appsettings.json`
- Console runner

## Requirements
- .NET 8 SDK
- For **OpenAI**: set environment variable `OPENAI_API_KEY`
- For **Ollama**: install Ollama, run `ollama serve`, pull a model like `ollama pull llama3.1:8b-instruct-q8_0`
- For **WebSearchTool**: get a [SerpAPI key](https://serpapi.com/), set `SERPAPI_KEY` env var

## Quick start
```bash
cd src/AgenticAI
dotnet run
```

Example interactions:
```
What time is it?
```
→ Calls `time_now`.

```
What is (3+5)*2 ?
```
→ Calls `calculator`.

```
Write 'Hello world' to notes/test.txt
```
→ Calls `file_write`.

```
Search the web for latest wildfire risk forecasts in Washington
```
→ Calls `web_search` and returns titles + links.

## Switching providers
Edit `src/AgenticAI/appsettings.json`:
```json
"Agent": { "Provider": "OpenAI", "Model": "gpt-4o-mini" }
```
For local dev with Ollama:
```json
"Agent": { "Provider": "Ollama", "Model": "llama3.1:8b-instruct-q8_0" }
```

## Adding your own tools
1. Implement `ITool`:
   ```csharp
   public class MyTool : ITool {
       public string Name => "my_tool";
       public string Description => "Does something";
       public JsonElement Schema => JsonDocument.Parse(@"{""type"":""object"",""properties"":{""x"":{""type"":""string""}}}").RootElement;
       public Task<object?> ExecuteAsync(Dictionary<string,object?> args, ToolContext ctx, CancellationToken ct=default) {
           var x = Convert.ToString(args["x"]);
           return Task.FromResult<object?>(new { echoed = x });
       }
   }
   ```
2. Register it in `Program.cs`:
   ```csharp
   services.AddSingleton<ITool, MyTool>();
   ```

## Protocol the model must follow
The LLM must output **only one of**:

Answer:
```json
{ "decision": "ANSWER", "final": "message" }
```

Tool call:
```json
{ "decision": "CALL_TOOL", "tool": "name", "arguments": { ... } }
```

## Configuration
- `Agent`: provider, model, max steps, system prompt
- `Memory`: file-based or null
- `Tools`: workspace folder for file operations

## Production notes
- Wrap HTTP calls with retry policies
- Swap memory to a database or vector store
- Validate JSON against schemas before execution
- Add structured logs and monitoring
- Sandbox tools that touch filesystem or network
