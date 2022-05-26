using Elsa.Expressions.Configuration;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Services;
using Elsa.JavaScript.Expressions;
using Elsa.JavaScript.Handlers;
using Elsa.JavaScript.Implementations;
using Elsa.JavaScript.Providers;
using Elsa.JavaScript.Services;
using Elsa.Mediator.Configuration;
using Elsa.Mediator.Extensions;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Attributes;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Configuration;

[Dependency(typeof(MediatorConfigurator))]
[Dependency(typeof(ExpressionsConfigurator))]
public class JavaScriptConfigurator : ConfiguratorBase
{
    public JavaScriptConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public override void Configure()
    {
        Services
            .AddSingleton<IExpressionSyntaxProvider, JavaScriptExpressionSyntaxProvider>()
            .AddSingleton<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddNotificationHandlersFrom<ConfigureJavaScriptEngineWithActivityOutput>()
            .AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>();
    }
}