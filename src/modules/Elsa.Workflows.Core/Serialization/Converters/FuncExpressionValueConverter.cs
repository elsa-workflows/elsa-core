using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Prevents System.Text.Json from trying to serialize the compiled delegate.
/// Always emits null and cannot rehydrate a Func.
/// </summary>
public class FuncExpressionValueConverter : JsonConverter<Func<ExpressionExecutionContext, ValueTask<object>>>
{
    public override Func<ExpressionExecutionContext, ValueTask<object>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Skip whatever value is in the JSON (probably null).
        if (reader.TokenType != JsonTokenType.Null)
            reader.Skip();

        // We can't deserialize a delegate, so return null.
        return null!;
    }

    public override void Write(Utf8JsonWriter writer, Func<ExpressionExecutionContext, ValueTask<object>> value, JsonSerializerOptions options)
    {
        // Emit a JSON null instead of trying to serialize the delegate
        writer.WriteNullValue();
    }
}
