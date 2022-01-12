using Elsa.Extensions;
using Elsa.Management.Contracts;
using Elsa.Scripting.JavaScript.Expressions;
using Elsa.Scripting.JavaScript.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJavaScriptExpressions(this IServiceCollection services)
    {
        return services
            .AddSingleton<IExpressionSyntaxProvider, JavaScriptExpressionSyntaxProvider>()
            .AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>();
    }
}