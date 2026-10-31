using CShells.Features;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.ShellFeatures;

/// <summary>
/// Installs and configures the expressions feature.
/// </summary>
[ManifestFeatureCategory("Expressions")]
[ShellFeature(
    "Expressions",
    DisplayName = "Expressions",
    Description = "Provides expression evaluation services")]
public class ExpressionsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddScoped<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>();
    }
}
