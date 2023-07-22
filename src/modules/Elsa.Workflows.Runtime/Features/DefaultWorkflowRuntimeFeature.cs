using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Installs the default runtime services.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class DefaultWorkflowRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public DefaultWorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowStateStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowStateStore> WorkflowStateStore { get; set; } = sp => ActivatorUtilities.CreateInstance<MemoryWorkflowStateStore>(sp);

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            // Replaceable factories
            .AddSingleton(WorkflowStateStore)

            // Memory stores.
            .AddMemoryStore<WorkflowState, MemoryWorkflowStateStore>()
            
            // Handlers.
            .AddNotificationHandler<DeleteWorkflowStates>()
            
            ;
    }
}