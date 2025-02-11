using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> and <see cref="WorkflowDefinitionsFeature"/> features with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(EFCoreWorkflowInstancePersistenceFeature))]
[DependsOn(typeof(EFCoreWorkflowDefinitionPersistenceFeature))]
[PublicAPI]
public class WorkflowManagementPersistenceFeature(IModule module) : PersistenceFeatureBase<WorkflowManagementPersistenceFeature, ManagementElsaDbContext>(module)
{
    public override Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilder
    {
        get => base.DbContextOptionsBuilder;
        set
        {
            base.DbContextOptionsBuilder = value;
            Module.Configure<EFCoreWorkflowDefinitionPersistenceFeature>(x => x.DbContextOptionsBuilder = value);
            Module.Configure<EFCoreWorkflowInstancePersistenceFeature>(x => x.DbContextOptionsBuilder = value);
        }
    }
}