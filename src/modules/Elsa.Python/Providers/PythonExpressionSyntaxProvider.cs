using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Python.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Python.Providers;

internal class PythonExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "Python";
    
    public ValueTask<IEnumerable<ExpressionDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var javaScript = CreatePythonDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionDescriptor>>(new[] { javaScript });
    }

    private ExpressionDescriptor CreatePythonDescriptor() => new()
    {
        Type = TypeName,
        DisplayName = "C#",
        HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<PythonExpressionHandler>
    };
}