using Elsa.Extensions;
using Elsa.ModularPersistence.Documents;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;
using Elsa.Workflows.Management.Persistence.ModularPersistence.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Extensions;

public static class WorkflowInstancesFeatureModularPersistenceExtensions
{
    public static WorkflowInstancesFeature UseModularPersistenceMetadata(this WorkflowInstancesFeature feature, Func<IServiceProvider, IDocumentStore> documentStoreFactory)
    {
        feature.Module.UseModularPersistence(modularPersistence => modularPersistence.RegisterManifest(WorkflowInstanceMetadataStorageManifest.Create()));
        feature.Module.Services.AddSingleton(documentStoreFactory);
        feature.Module.Services.AddScoped<IWorkflowInstanceMetadataStore, ModularPersistenceWorkflowInstanceMetadataStore>();
        return feature;
    }
}
