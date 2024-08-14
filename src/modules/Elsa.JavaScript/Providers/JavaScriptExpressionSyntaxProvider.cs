using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Expressions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Providers;

[UsedImplicitly]
internal class JavaScriptExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "JavaScript";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "JavaScript",
            Properties = new { MonacoLanguage = "javascript" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<JavaScriptExpressionHandler>
        };
    }
}