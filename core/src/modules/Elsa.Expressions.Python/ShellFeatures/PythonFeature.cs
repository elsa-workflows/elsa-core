using CShells.Features;
using Elsa.Common.ShellFeatures;
using Elsa.Expressions.Python.Activities;
using Elsa.Expressions.Python.ActivityDescriptorModifiers;
using Elsa.Expressions.Python.Contracts;
using Elsa.Expressions.Python.HostedServices;
using Elsa.Expressions.Python.Options;
using Elsa.Expressions.Python.Providers;
using Elsa.Expressions.Python.Services;
using Elsa.Expressions.ShellFeatures;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Python.ShellFeatures;

/// <summary>
/// Installs Python integration.
/// </summary>
[ManifestFeatureCategory("Expressions")]
[ManifestFeatureCategory("Scripting")]
[ShellFeature(
    DisplayName = "Python Expressions",
    Description = "Provides Python expression evaluation capabilities for workflows",
    DependsOn = [typeof(MediatorFeature), typeof(ExpressionsFeature)])]
[UsedImplicitly]
public class PythonFeature : IShellFeature
{
    /// <summary>
    /// Configures the <see cref="Options.PythonOptions"/>.
    /// </summary>
    public Action<PythonOptions> PythonOptions { get; set; } = _ => { };

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure(PythonOptions);

        // Python services.
        services
            .AddScoped<IPythonEvaluator, PythonNetPythonEvaluator>()
            .AddExpressionDescriptorProvider<PythonExpressionDescriptorProvider>()
            .AddSingleton<IActivityDescriptorModifier, PythonActivityDescriptorModifier>();

        // Handlers.
        services.AddNotificationHandlersFrom<PythonFeature>();

        // UI property handlers.
        services.AddScoped<IPropertyUIHandler, RunPythonOptionsProvider>();

        // Hosted services.
        services.AddHostedService<PythonGlobalInterpreterManager>();
    }
}

