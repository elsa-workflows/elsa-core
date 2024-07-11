using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;

namespace Elsa.Expressions.Models;

/// <summary>
/// Describes an expression type.
/// </summary>
public class ExpressionDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionDescriptor"/> class.
    /// </summary>
    public ExpressionDescriptor()
    {
        // Default deserialization function.
        Deserialize = context =>
        {
            var expression = new Expression(context.ExpressionType, null);

            if (context.JsonElement.ValueKind == JsonValueKind.Object)
                if (context.JsonElement.TryGetProperty("value", out var expressionValueElement)) 
                    expression.Value = expressionValueElement.GetValue();

            return expression;
        };
    }

    /// <summary>
    /// Gets or sets the syntax name.
    /// </summary>
    public string Type { get; init; } = default!;

    /// <summary>
    /// Gets or sets the display name of the expression type.
    /// </summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether the expression value is serializable.
    /// </summary>
    public bool IsSerializable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the expression type is browsable.
    /// </summary>
    public bool IsBrowsable { get; set; } = true;

    /// <summary>
    /// Gets or sets the expression type properties.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the expression handler factory.
    /// </summary>
    public Func<IServiceProvider, IExpressionHandler> HandlerFactory { get; set; } = default!;

    /// <summary>
    /// Gets or sets the memory block reference factory.
    /// </summary>
    public Func<MemoryBlockReference> MemoryBlockReferenceFactory { get; set; } = () => new MemoryBlockReference();

    /// <summary>
    /// Gets or sets the expression deserialization function.
    /// </summary>
    public Func<ExpressionSerializationContext, Expression> Deserialize { get; set; } = default!;
}