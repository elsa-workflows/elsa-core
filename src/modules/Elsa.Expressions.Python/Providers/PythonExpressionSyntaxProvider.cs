using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Expressions.Python.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Python.Providers;

internal class PythonExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "Python";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "Python",
            Properties = new { MonacoLanguage = "python" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<PythonExpressionHandler>
        };
    }
}