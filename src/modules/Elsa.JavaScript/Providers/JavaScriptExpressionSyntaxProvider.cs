using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.JavaScript.Expressions;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Providers;

internal class JavaScriptExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string TypeName = "JavaScript";
    
    public ValueTask<IEnumerable<ExpressionDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var javaScript = CreateJavaScriptDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionDescriptor>>(new[] { javaScript });
    }

    private static ExpressionDescriptor CreateJavaScriptDescriptor() => new()
    {
        Type = TypeName,
        DisplayName = "JavaScript",
        HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<JavaScriptExpressionHandler>
    };
}