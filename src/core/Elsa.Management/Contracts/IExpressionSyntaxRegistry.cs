using Elsa.Management.Models;

namespace Elsa.Management.Contracts;

public interface IExpressionSyntaxRegistry
{
    void Add(ExpressionSyntaxDescriptor descriptor);
    void AddMany(IEnumerable<ExpressionSyntaxDescriptor> descriptors);
    IEnumerable<ExpressionSyntaxDescriptor> ListAll();
    ExpressionSyntaxDescriptor? Find(Func<ExpressionSyntaxDescriptor, bool> predicate);
    ExpressionSyntaxDescriptor? Find(string syntax);
}