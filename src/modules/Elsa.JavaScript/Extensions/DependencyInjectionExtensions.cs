using Elsa.Expressions.Extensions;
using Elsa.Expressions.Services;
using Elsa.JavaScript.Expressions;
using Elsa.JavaScript.Handlers;
using Elsa.JavaScript.Implementations;
using Elsa.JavaScript.Providers;
using Elsa.JavaScript.Services;
using Elsa.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddJavaScriptExpressions(this IServiceCollection services)
    {
        return services
            .AddSingleton<IExpressionSyntaxProvider, JavaScriptExpressionSyntaxProvider>()
            .AddSingleton<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddNotificationHandlersFrom<ConfigureJavaScriptEngineWithActivityOutput>()
            .AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>();
    }
}