using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Enables storage of workflow instances.
/// </summary>
public class WorkflowInstancesFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// The factory to create new instances of <see cref="IWorkflowInstanceStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>();

    private KeyValuePair<string, WorkflowPayloadPersistenceMode>? PayloadPersistence { get; set; }

    public WorkflowInstancesFeature SetPayloadPersistence(string payloadType, WorkflowPayloadPersistenceMode mode)
    {
        PayloadPersistence = new(payloadType, mode);
        return this;
    }

    /// <inheritdoc />
    public override void Apply()
    {        
        Services.AddScoped(WorkflowInstanceStore);
        Services.Configure<WorkflowPayloadOptions>(options => options.WorkflowInstancesPersistence = PayloadPersistence);
    }
}