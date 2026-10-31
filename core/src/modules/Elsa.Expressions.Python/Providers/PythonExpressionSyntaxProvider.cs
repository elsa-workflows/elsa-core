using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Expressions.Python.Expressions;
using Elsa.Expressions.Python.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.Python.Providers;

internal class PythonExpressionDescriptorProvider(IOptions<PythonOptions> options) : IExpressionDescriptorProvider
{
    private const string TypeName = "Python";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "Python",
            IsBrowsable = options.Value.AllowHostCodeExecution,
            Properties = new { MonacoLanguage = "python" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<PythonExpressionHandler>
        };
    }
}
