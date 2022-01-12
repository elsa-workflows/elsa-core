using Elsa.Extensions;
using Elsa.Management.Contracts;
using Elsa.Scripting.JavaScript.Contracts;
using Elsa.Scripting.JavaScript.Expressions;
using Elsa.Scripting.JavaScript.Providers;
using Elsa.Scripting.JavaScript.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJavaScriptExpressions(this IServiceCollection services)
    {
        return services
            .AddSingleton<IExpressionSyntaxProvider, JavaScriptExpressionSyntaxProvider>()
            .AddSingleton<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>();
    }
}