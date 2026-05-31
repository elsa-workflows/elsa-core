using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleLogStreaming.Core.Models;

namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

/// <summary>
/// Converts the public console stream filter contract values.
/// </summary>
public sealed class ConsoleStreamJsonConverter : JsonConverter<ConsoleStream?>
{
    public override ConsoleStream? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => ReadString(reader.GetString()),
            JsonTokenType.Number => ReadNumber(ref reader),
            _ => throw new JsonException("The console log stream filter must be 'stdout', 'stderr', or null.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ConsoleStream? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value switch
        {
            ConsoleStream.Stdout => "stdout",
            ConsoleStream.Stderr => "stderr",
            _ => throw new JsonException($"Unsupported console stream value '{value}'.")
        });
    }

    private static ConsoleStream? ReadString(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "all" => null,
            "stdout" => ConsoleStream.Stdout,
            "stderr" => ConsoleStream.Stderr,
            _ => throw new JsonException("The console log stream filter must be 'stdout', 'stderr', or null.")
        };
    }

    private static ConsoleStream ReadNumber(ref Utf8JsonReader reader)
    {
        if (!reader.TryGetInt32(out var value) || !Enum.IsDefined(typeof(ConsoleStream), value))
            throw new JsonException("The console log stream filter has an unsupported numeric value.");

        return (ConsoleStream)value;
    }
}
