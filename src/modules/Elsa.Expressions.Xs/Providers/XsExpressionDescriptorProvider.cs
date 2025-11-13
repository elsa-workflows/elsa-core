using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Expressions.Xs.Expressions;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Xs.Providers;

internal class XsExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "XS";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "XS Script",
            Properties = new { MonacoLanguage = "csharp" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<XsExpressionHandler>
        };
    }
}
