using Elsa.Labels.Implementations;
using Elsa.Labels.Services;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.Options;

public class LabelPersistenceOptions : ConfiguratorBase
{
    public ElsaOptionsConfigurator ElsaOptionsConfigurator { get; }

    public LabelPersistenceOptions(ElsaOptionsConfigurator elsaOptionsConfigurator)
    {
        ElsaOptionsConfigurator = elsaOptionsConfigurator;
    }
    
    public Func<IServiceProvider, ILabelStore> LabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryLabelStore>();
    public Func<IServiceProvider, IWorkflowDefinitionLabelStore> WorkflowDefinitionLabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryWorkflowDefinitionLabelStore>();
    
    public LabelPersistenceOptions WithLabelStore(Func<IServiceProvider, ILabelStore> factory)
    {
        LabelStore = factory;
        return this;
    }
    
    public LabelPersistenceOptions WithWorkflowDefinitionLabelStore(Func<IServiceProvider, IWorkflowDefinitionLabelStore> factory)
    {
        WorkflowDefinitionLabelStore = factory;
        return this;
    }

    public override void ConfigureServices(ElsaOptionsConfigurator configurator)
    {
        configurator.Services
            .AddSingleton(LabelStore)
            .AddSingleton(WorkflowDefinitionLabelStore)
            ;
    }
}