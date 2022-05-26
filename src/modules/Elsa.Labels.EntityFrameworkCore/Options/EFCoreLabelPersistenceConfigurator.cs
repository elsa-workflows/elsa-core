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
    
    public override void ConfigureServices(IServiceConfiguration serviceConfiguration)
    {
        base.ConfigureServices(serviceConfiguration);

        serviceConfiguration.UseLabels(labels => labels
            .WithLabelStore(sp => sp.GetRequiredService<EFCoreLabelStore>())
            .WithWorkflowDefinitionLabelStore(sp => sp.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>())
        );

        var services = serviceConfiguration.Services;
        AddStore<Label, EFCoreLabelStore>(services);
        AddStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>(services);
    }
}