using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Serialization.Converters;

public class FuncExpressionConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type t) => t == typeof(Func<ExpressionExecutionContext, ValueTask<object>>);

    public override JsonConverter CreateConverter(Type t, JsonSerializerOptions opts) => new FuncExpressionValueConverter();
}
