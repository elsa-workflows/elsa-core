using Elsa.Workflows.Management.Services;
using Elsa.Mediator.Extensions;
using Elsa.Scripting.JavaScript.Expressions;
using Elsa.Scripting.JavaScript.Handlers;
using Elsa.Scripting.JavaScript.Implementations;
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
            .AddNotificationHandlersFrom<ConfigureJavaScriptEngineWithActivityOutput>()
            .AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>();
    }
}