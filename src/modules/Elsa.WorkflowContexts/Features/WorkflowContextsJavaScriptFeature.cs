using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Features;
using Elsa.JavaScript.Notifications;
using Elsa.WorkflowContexts.Scripting.JavaScript;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.Features;

/// <summary>
/// Enabled when both <see cref="WorkflowContextsFeature"/> and <see cref="JavaScriptFeature"/> are enabled.
/// </summary>
[DependencyOf(typeof(WorkflowContextsFeature))]
[DependencyOf(typeof(JavaScriptFeature))]
public class WorkflowContextsJavaScriptFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowContextsJavaScriptFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<ConfigureJavaScriptEngine>();
        Services.AddNotificationHandler<ConfigureJavaScriptEngine, EvaluatingJavaScript>(sp => sp.GetRequiredService<ConfigureJavaScriptEngine>());
        Services.AddTypeDefinitionProvider<ConfigureJavaScriptEngine>(sp => sp.GetRequiredService<ConfigureJavaScriptEngine>());
        Services.AddFunctionDefinitionProvider<ConfigureJavaScriptEngine>(sp => sp.GetRequiredService<ConfigureJavaScriptEngine>());
    }
}