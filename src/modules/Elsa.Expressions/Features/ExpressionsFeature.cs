using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Features;

public class ExpressionsFeature : FeatureBase
{
    public ExpressionsFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Services
            .AddSingleton<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IExpressionHandlerRegistry, ExpressionHandlerRegistry>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>();
    }
}