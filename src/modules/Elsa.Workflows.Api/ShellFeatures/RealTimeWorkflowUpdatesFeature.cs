using CShells.Features;
using Elsa.Workflows.Api.RealTime.Handlers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.ShellFeatures;

/// <summary>
/// Sets up a SignalR hub for receiving workflow events on the client.
/// </summary>
[ShellFeature(
    DisplayName = "Real-Time Workflow Updates",
    Description = "Provides real-time workflow updates via SignalR")]
[UsedImplicitly]
public class RealTimeWorkflowUpdatesFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR();
        services.AddNotificationHandler<BroadcastWorkflowProgress>();
    }
}


