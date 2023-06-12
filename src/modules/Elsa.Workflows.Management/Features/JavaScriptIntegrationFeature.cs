using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Features;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.HostedServices;
using Elsa.Workflows.Management.Scripting.JavaScript;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Installs JavaScript activities.
/// </summary>
[DependsOn(typeof(JavaScriptFeature))]
[PublicAPI]
public class JavaScriptIntegrationFeature : FeatureBase
{
    /// <inheritdoc />
    public JavaScriptIntegrationFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.UseWorkflowManagement(management => management.AddActivity<RunJavaScript>());
    }
    
    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        ConfigureHostedService<RegisterVariableTypesWithJavaScriptHostedService>();
    }
    
    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddSingleton<IActivityPropertyOptionsProvider, RunJavaScriptOptionsProvider>()
            .AddFunctionDefinitionProvider<InputFunctionsDefinitionProvider>();
    }
}