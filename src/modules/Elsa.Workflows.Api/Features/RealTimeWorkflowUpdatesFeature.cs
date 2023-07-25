using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Api.RealTime.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Features;

/// <summary>
/// Sets up a SignalR hub for receiving workflow events on the client.
/// </summary>
public class RealTimeWorkflowUpdatesFeature : FeatureBase
{
    /// <inheritdoc />
    public RealTimeWorkflowUpdatesFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSignalR();
        Services.AddNotificationHandler<BroadcastWorkflowProgress>();
    }
}