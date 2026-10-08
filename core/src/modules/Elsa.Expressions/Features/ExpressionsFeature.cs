using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Features;

/// <summary>
/// Installs and configures the expressions feature.
/// </summary>
public class ExpressionsFeature : FeatureBase
{
    /// <inheritdoc />
    public ExpressionsFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>();
    }
}