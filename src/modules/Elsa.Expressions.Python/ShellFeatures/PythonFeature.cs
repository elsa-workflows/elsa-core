using CShells.Features;
using Elsa.Expressions.Python.Activities;
using Elsa.Expressions.Python.Contracts;
using Elsa.Expressions.Python.HostedServices;
using Elsa.Expressions.Python.Options;
using Elsa.Expressions.Python.Providers;
using Elsa.Expressions.Python.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Python.ShellFeatures;

/// <summary>
/// Installs Python integration.
/// </summary>
[ShellFeature(
    DisplayName = "Python Expressions",
    Description = "Provides Python expression evaluation capabilities for workflows",
    DependsOn = ["Mediator", "Expressions"])]
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
            .AddExpressionDescriptorProvider<PythonExpressionDescriptorProvider>();

        // Handlers.
        services.AddNotificationHandlersFrom<PythonFeature>();

        // UI property handlers.
        services.AddScoped<IPropertyUIHandler, RunPythonOptionsProvider>();

        // Hosted services.
        services.AddHostedService<PythonGlobalInterpreterManager>();
    }
}


