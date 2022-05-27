using Elsa.Expressions.Extensions;
using Elsa.Expressions.Features;
using Elsa.Expressions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Expressions;
using Elsa.JavaScript.Handlers;
using Elsa.JavaScript.Implementations;
using Elsa.JavaScript.Providers;
using Elsa.JavaScript.Services;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Features;

[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class JavaScriptFeature : FeatureBase
{
    public JavaScriptFeature(IModule module) : base(module)
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