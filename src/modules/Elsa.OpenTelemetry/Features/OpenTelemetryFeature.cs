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
    /// <remarks>This is needed when middleware is active that uses the previous Parent Activity despite creating an Activity based on an empty Parent.</remarks>
    public bool UseDummyParentActivityAsRootSpan { get; set; } = false;
    
    public override void Configure()
    {
        Services
            .AddScoped<IErrorSpanHandler, DefaultErrorSpanHandler>()
            .AddScoped<IErrorSpanHandler, FaultExceptionErrorSpanHandler>();

        Services.Configure<OpenTelemetryOptions>(options =>
        {
            options.UseNewRootActivityForRemoteParent = UseNewRootActivityForRemoteParent;
            options.UseDummyParentActivityAsRootSpan = UseDummyParentActivityAsRootSpan;
        });
    }
}