using Elsa.Expressions.Models;
using Elsa.Expressions.Services;

namespace Elsa.Workflows.Management.Services;

public class ExpressionSyntaxRegistry : IExpressionSyntaxRegistry
{
    private readonly IDictionary<string, ExpressionSyntaxDescriptor> _expressionSyntaxDescriptors = new Dictionary<string, ExpressionSyntaxDescriptor>();
    public void Add(ExpressionSyntaxDescriptor descriptor) => _expressionSyntaxDescriptors[descriptor.Syntax] = descriptor;

    public void AddMany(IEnumerable<ExpressionSyntaxDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
            Add(descriptor);
    }

    public IEnumerable<ExpressionSyntaxDescriptor> ListAll() => _expressionSyntaxDescriptors.Values;
    public ExpressionSyntaxDescriptor? Find(Func<ExpressionSyntaxDescriptor, bool> predicate) => _expressionSyntaxDescriptors.Values.FirstOrDefault(predicate);
    public ExpressionSyntaxDescriptor? Find(string syntax) => Find(x => x.Syntax == syntax);
}