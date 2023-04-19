using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Features;
using Elsa.JavaScript.Notifications;
using Elsa.WorkflowContexts.Scripting.JavaScript;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.Features;

/// <summary>
/// A feature that adds support for workflow context providers.
/// </summary>
[PublicAPI]
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(JavaScriptFeature))]
public class WorkflowContextsFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowContextsFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<WorkflowContextsFeature>();
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<ConfigureJavaScriptEngine>();
        Services.AddNotificationHandler<ConfigureJavaScriptEngine, EvaluatingJavaScript>(sp => sp.GetRequiredService<ConfigureJavaScriptEngine>());
        Services.AddTypeDefinitionProvider<ConfigureJavaScriptEngine>(sp => sp.GetRequiredService<ConfigureJavaScriptEngine>());
        Services.AddFunctionDefinitionProvider<ConfigureJavaScriptEngine>(sp => sp.GetRequiredService<ConfigureJavaScriptEngine>());
    }
}