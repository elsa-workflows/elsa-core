using Elsa.Expressions.Contracts;

namespace Elsa.Expressions.Models;

/// <summary>
/// Describes an expression type.
/// </summary>
public class ExpressionDescriptor
{
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
}