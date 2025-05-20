using Elsa.Common.Features;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Scripting.Python.Activities;
using Elsa.Scripting.Python.Contracts;
using Elsa.Scripting.Python.HostedServices;
using Elsa.Scripting.Python.Options;
using Elsa.Scripting.Python.Providers;
using Elsa.Scripting.Python.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.Python.Features;

/// <summary>
/// Installs Python integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class PythonFeature : FeatureBase
{
    /// <inheritdoc />
    public PythonFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Configures the <see cref="Options.PythonOptions"/>.
    /// </summary>
    public Action<PythonOptions> PythonOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<PythonGlobalInterpreterManager>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(PythonOptions);

        // Python services.
        Services
            .AddScoped<IPythonEvaluator, PythonNetPythonEvaluator>()
            .AddExpressionDescriptorProvider<PythonExpressionDescriptorProvider>()
            ;

        // Handlers.
        Services.AddNotificationHandlersFrom<PythonFeature>();

        // Activities.
        Module.AddActivitiesFrom<PythonFeature>();
        
        // UI property handlers.
        Services.AddScoped<IPropertyUIHandler, RunPythonOptionsProvider>();
    }
}