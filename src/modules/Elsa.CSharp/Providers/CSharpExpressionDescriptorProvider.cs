using Elsa.CSharp.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.CSharp.Providers;

internal class CSharpExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "CSharp";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "C#",
            Properties = new { MonacoLanguage = "csharp" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<CSharpExpressionHandler>
        };
    }
}