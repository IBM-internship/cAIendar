// Llm/CommonJson.cs

using System.Text.Json;
using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Llm;

internal static class CommonJson
{
    internal static PromptResponse ParseResponse(JsonElement root)
    {
        var msg = root.GetProperty("choices")
            .EnumerateArray()
            .First()
            .GetProperty("message");

        // Watsonx omits "content" when the assistant exclusively returns tool calls.
        string? txt = null;
        if (msg.TryGetProperty("content", out var ct) &&
            ct.ValueKind != JsonValueKind.Null &&
            ct.ValueKind != JsonValueKind.Undefined)
        {
            txt = ct.GetString();
        }

        // Collect any tool calls.
        List<ToolCall>? calls = null;
        if (!msg.TryGetProperty("tool_calls", out var tc) ||
            tc.ValueKind != JsonValueKind.Array) return new PromptResponse(txt, calls);
        calls = [];
        calls.AddRange(from e in tc.EnumerateArray()
            let fn = e.GetProperty("function")
            let id = e.GetProperty("id").GetString()!
            let name = fn.GetProperty("name").GetString()!
            let args = fn.GetProperty("arguments")
            select new ToolCall(id, name, args));

        return new PromptResponse(txt, calls);
    }
}