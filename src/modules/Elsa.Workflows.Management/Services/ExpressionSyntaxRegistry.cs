using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class ExpressionSyntaxRegistry : IExpressionSyntaxRegistry
{
    private readonly IDictionary<string, ExpressionSyntaxDescriptor> _expressionSyntaxDescriptors = new Dictionary<string, ExpressionSyntaxDescriptor>();

    /// <inheritdoc />
    public void Add(ExpressionSyntaxDescriptor descriptor) => _expressionSyntaxDescriptors[descriptor.Syntax] = descriptor;

    /// <inheritdoc />
    public void AddMany(IEnumerable<ExpressionSyntaxDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
            Add(descriptor);
    }

    /// <inheritdoc />
    public IEnumerable<ExpressionSyntaxDescriptor> ListAll() => _expressionSyntaxDescriptors.Values;

    /// <inheritdoc />
    public ExpressionSyntaxDescriptor? Find(Func<ExpressionSyntaxDescriptor, bool> predicate) => _expressionSyntaxDescriptors.Values.FirstOrDefault(predicate);

    /// <inheritdoc />
    public ExpressionSyntaxDescriptor? Find(string syntax) => Find(x => x.Syntax == syntax);
}