using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.ProtoActor.ShellFeatures;

/// <summary>
/// Shell feature for ProtoActor runtime.
/// </summary>
[ShellFeature(
    DisplayName = "ProtoActor Runtime",
    Description = "Configures ProtoActor as the distributed workflow runtime",
    DependsOn = ["WorkflowRuntime"])]
[UsedImplicitly]
public class ProtoActorRuntimeShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }
}

