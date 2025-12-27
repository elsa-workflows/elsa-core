using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Configures workflow definition storage.
/// </summary>
public class WorkflowDefinitionsFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// The factory to create new instances of <see cref="IWorkflowDefinitionStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();

    private KeyValuePair<string, WorkflowPayloadPersistenceMode>? PayloadPersistence { get; set; }

    public WorkflowDefinitionsFeature SetPayloadPersistence(string payloadType, WorkflowPayloadPersistenceMode mode)
    {
        PayloadPersistence = new(payloadType, mode);
        return this;
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddScoped(WorkflowDefinitionStore);

        Services.Configure<WorkflowPayloadOptions>(options => options.WorkflowDefinitionsPersistence = PayloadPersistence);
    }
}