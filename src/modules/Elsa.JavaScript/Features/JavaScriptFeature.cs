using Elsa.Expressions.Extensions;
using Elsa.Expressions.Features;
using Elsa.Expressions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Expressions;
using Elsa.JavaScript.Implementations;
using Elsa.JavaScript.Providers;
using Elsa.JavaScript.Services;
using Elsa.Mediator.Features;
using Elsa.Workflows.Management.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Features;

[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class JavaScriptFeature : FeatureBase
{
    public JavaScriptFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Services
            .AddSingleton<IExpressionSyntaxProvider, JavaScriptExpressionSyntaxProvider>()
            .AddSingleton<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddSingleton<IActivityPropertyOptionsProvider, RunJavaScriptOptionsProvider>()
            .AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>();
    }
}