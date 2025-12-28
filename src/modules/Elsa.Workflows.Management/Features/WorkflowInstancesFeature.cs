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

    public PayloadPersistenceOption? PayloadPersistence { get; set; }

    public WorkflowInstancesFeature SetPayloadPersistence(string payloadTypeIdentifier, PayloadPersistenceMode mode)
    {
        PayloadPersistence = new(payloadTypeIdentifier, mode);
        return this;
    }

    /// <inheritdoc />
    public override void Apply()
    {        
        Services.AddScoped(WorkflowInstanceStore);
        Services.Configure<PayloadOptions>(options => options.WorkflowInstancesPersistence = PayloadPersistence);
    }
}