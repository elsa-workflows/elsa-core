using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Converters;

/// <summary>
/// Converts <see cref="IExpression"/> to and from JSON.
/// </summary>
public class ExpressionJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IExpression);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new ExpressionJsonConverter();
}