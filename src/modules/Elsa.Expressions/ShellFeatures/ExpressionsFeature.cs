using CShells.Features;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.ShellFeatures;

/// <summary>
/// Installs and configures the expressions feature.
/// </summary>
public class ExpressionsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>();
    }
}