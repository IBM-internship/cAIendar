using System.Text.Json;
using PromptingPipeline.Models;

namespace PromptingPipeline.Llm;

internal static class CommonJson
{
    internal static PromptResponse ParseResponse(JsonElement root)
    {
        var msg  = root.GetProperty("choices").EnumerateArray().First().GetProperty("message");
        var txt  = msg.GetProperty("content").GetString();

        List<ToolCall>? calls = null;
        if (msg.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
        {
            calls = new();
            foreach (var e in tc.EnumerateArray())
            {
                var fn   = e.GetProperty("function");
                var id   = e.GetProperty("id").GetString()!;
                var name = fn.GetProperty("name").GetString()!;
                var args = fn.GetProperty("arguments");
                calls.Add(new ToolCall(id, name, args));
            }
        }

        return new PromptResponse(txt, calls);
    }
}
