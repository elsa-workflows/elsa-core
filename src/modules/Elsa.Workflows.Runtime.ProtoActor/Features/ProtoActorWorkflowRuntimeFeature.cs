using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.ProtoActor;
using Elsa.ProtoActor.Features;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Features;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.ProtoActor.Actors;
using Elsa.Workflows.Runtime.ProtoActor.Mappers;
using Elsa.Workflows.Runtime.ProtoActor.Providers;
using Elsa.Workflows.Runtime.ProtoActor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.ProtoActor.Features;

/// <summary>
/// Installs the Proto Actor feature to host &amp; execute workflow instances.
/// </summary>
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(ProtoActorFeature))]
public class ProtoActorWorkflowRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public ProtoActorWorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // Configure runtime with ProtoActor workflow runtime.
        Module.Configure<WorkflowRuntimeFeature>().WorkflowRuntime = sp => ActivatorUtilities.CreateInstance<ProtoActorWorkflowRuntime>(sp);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var services = Services;

        // Mappers.
        services
            .AddSingleton<Mappers.Mappers>()
            .AddSingleton<ExceptionMapper>()
            .AddSingleton<ActivityHandleMapper>()
            .AddSingleton<WorkflowDefinitionHandleMapper>()
            .AddSingleton<ActivityIncidentMapper>()
            .AddSingleton<WorkflowStatusMapper>()
            .AddSingleton<WorkflowSubStatusMapper>()
            .AddSingleton<CreateWorkflowInstanceRequestMapper>()
            .AddSingleton<CreateWorkflowInstanceResponseMapper>()
            .AddSingleton<RunWorkflowInstanceRequestMapper>()
            .AddSingleton<RunWorkflowInstanceResponseMapper>()
            .AddSingleton<CreateAndRunWorkflowInstanceRequestMapper>()
            .AddSingleton<RunWorkflowParamsMapper>()
            .AddSingleton<WorkflowStateJsonMapper>();

        // Actor Providers.
        services.AddSingleton<IVirtualActorsProvider, WorkflowInstanceVirtualActorProvider>();
        
        // Actors.
        services
            .AddTransient(sp => new WorkflowInstanceActor((context, _) => ActivatorUtilities.CreateInstance<WorkflowInstance>(sp, context)));

        // Distributed runtime.
        services.AddSingleton<ProtoActorWorkflowRuntime>();
    }
}