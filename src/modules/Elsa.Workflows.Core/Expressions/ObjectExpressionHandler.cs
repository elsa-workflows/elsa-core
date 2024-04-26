using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Common.Converters;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Expressions;

/// <summary>
/// Evaluates an object expression.
/// </summary>
[UsedImplicitly]
public class ObjectExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    public ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var value = expression.Value.ConvertTo<string>() ?? "";

        if (string.IsNullOrWhiteSpace(value))
            return ValueTask.FromResult(default(object?));

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        serializerOptions.Converters.Add(new IntegerJsonConverter());
        serializerOptions.Converters.Add(new DecimalJsonConverter());
        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        
        var converterOptions = new ObjectConverterOptions(serializerOptions);
        var model = value.ConvertTo(returnType, converterOptions);
        return ValueTask.FromResult(model);
    }
}