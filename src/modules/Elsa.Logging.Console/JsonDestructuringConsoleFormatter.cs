using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Elsa.Logging.Console;

/// <summary>
/// A custom console formatter that formats log entries as JSON with destructured data.
/// </summary>
/// <remarks>
/// This formatter outputs log information in a JSON format, including details such as log level, category, message,
/// state, and scopes. The formatter is designed to provide a structured and easily parseable log output for improved log analysis.
/// </remarks>
public sealed class JsonDestructuringConsoleFormatter() : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "json-destructuring";
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopes, TextWriter writer)
    {
        using var stream = new MemoryStream();
        using var json = new Utf8JsonWriter(stream, new()
        {
            Indented = false
        });

        json.WriteStartObject();
        json.WriteString("LogLevel", logEntry.LogLevel.ToString());
        json.WriteString("Category", logEntry.Category);
        json.WriteString("Message", logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception));

        // STATE (TState might be IReadOnlyList<KeyValuePair<string,object?>>)
        if (logEntry.State is IEnumerable<KeyValuePair<string, object?>> kvs)
        {
            json.WriteStartObject("State");
            foreach (var kv in kvs)
            {
                if (kv.Key == "{OriginalFormat}")
                {
                    json.WriteString(kv.Key, kv.Value?.ToString());
                    continue;
                }

                json.WritePropertyName(kv.Key);
                JsonSerializer.Serialize(json, kv.Value);
            }

            json.WriteEndObject();
        }

        // SCOPES
        if (scopes is not null)
        {
            json.WriteStartArray("Scopes");
            scopes.ForEachScope((scope, state) =>
            {
                switch (scope)
                {
                    case IEnumerable<KeyValuePair<string, object?>> scopeKvs:
                        state.WriteStartObject();
                        foreach (var kv in scopeKvs)
                        {
                            state.WritePropertyName(kv.Key);
                            JsonSerializer.Serialize(state, kv.Value);
                        }

                        state.WriteEndObject();
                        break;
                    default:
                        JsonSerializer.Serialize(state, scope);
                        break;
                }
            }, json);
            json.WriteEndArray();
        }

        json.WriteEndObject();
        json.Flush();

        // Write the JSON string to the TextWriter
        writer.Write(Encoding.UTF8.GetString(stream.ToArray()));
        writer.WriteLine();
    }
}