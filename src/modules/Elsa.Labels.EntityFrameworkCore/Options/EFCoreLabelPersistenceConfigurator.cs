using Elsa.Labels.Entities;
using Elsa.Labels.EntityFrameworkCore.Implementations;
using Elsa.Labels.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.EntityFrameworkCore.Options;

public class EFCoreLabelPersistenceConfigurator : EFCorePersistenceConfigurator<LabelsDbContext>
{
    public EFCoreLabelPersistenceConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }
    
    public override void ConfigureServices()
    {
        base.ConfigureServices();

        ServiceConfiguration.UseLabels(labels => labels
            .WithLabelStore(sp => sp.GetRequiredService<EFCoreLabelStore>())
            .WithWorkflowDefinitionLabelStore(sp => sp.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>())
        );
        
        AddStore<Label, EFCoreLabelStore>(Services);
        AddStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>(Services);
    }
}