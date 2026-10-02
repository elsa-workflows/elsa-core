using Elsa.Api.Client.Resources.Scripting.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Provides expression descriptors that can be used as a cascading parameter.
/// </summary>
public class ExpressionDescriptorProvider
{
    private readonly IDictionary<string, ExpressionDescriptor> _expressionDescriptors = new Dictionary<string, ExpressionDescriptor>();

    /// <summary>
    /// Lists all expression descriptors.
    /// </summary>
    public IEnumerable<ExpressionDescriptor> ListDescriptors() => _expressionDescriptors.Values;

    /// <summary>
    /// Adds an expression descriptor.
    /// </summary>
    public void Add(ExpressionDescriptor descriptor) => _expressionDescriptors[descriptor.Type] = descriptor;

    /// <summary>
    /// Gets an expression descriptor by type.
    /// </summary>
    public ExpressionDescriptor? GetByType(string type) => _expressionDescriptors.TryGetValue(type, out var descriptor) ? descriptor : default;

    /// <summary>
    /// Adds a range of expression descriptors.
    /// </summary>
    public void AddRange(IEnumerable<ExpressionDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
            Add(descriptor);
    }
}