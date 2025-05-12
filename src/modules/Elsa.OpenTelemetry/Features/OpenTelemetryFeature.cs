using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Handlers;
using Elsa.OpenTelemetry.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.OpenTelemetry.Features;

public class OpenTelemetryFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// When active this will create a new root Activity if the current Activity has a remote parent.
    /// </summary>
    public bool UseNewRootActivityForRemoteParent { get; set; } = true;
    
    /// <summary>
    /// Determines if instead of a empty parent, a dummy parent Activity should be used to create a new Root Activity.
    /// </summary>
    /// <remarks>This is needed for Datadog middleware that uses the previous parent Activity despite creating an Activity based on an empty parent.</remarks>
    public bool UseDummyParentActivityAsRootSpan { get; set; } = false;
    
    public override void Configure()
    {
        Services
            .AddScoped<IActivityErrorSpanHandler, DefaultErrorSpanHandler>()
            .AddScoped<IActivityErrorSpanHandler, FaultExceptionErrorSpanHandler>()
            .AddScoped<IWorkflowErrorSpanHandler, DefaultErrorSpanHandler>()
            .AddScoped<IWorkflowErrorSpanHandler, FaultExceptionErrorSpanHandler>();

        Services.Configure<OpenTelemetryOptions>(options =>
        {
            options.UseNewRootActivityForRemoteParent = UseNewRootActivityForRemoteParent;
            options.UseDummyParentActivityAsRootSpan = UseDummyParentActivityAsRootSpan;
        });
    }
}