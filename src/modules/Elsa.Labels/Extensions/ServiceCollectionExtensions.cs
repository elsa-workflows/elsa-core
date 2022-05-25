using Elsa.Labels.Entities;
using Elsa.Labels.Implementations;
using Elsa.Persistence.Common.Extensions;
using Elsa.Workflows.Core.Options;

#pragma warning disable CS8631

namespace Elsa.Labels.Extensions;

public static class ServiceCollectionExtensions
{
    public static ElsaOptionsConfigurator AddLabels(this ElsaOptionsConfigurator configurator)
    {
        var services = configurator.Services;

        services
            .AddMemoryStore<Label, InMemoryLabelStore>()
            .AddMemoryStore<WorkflowDefinitionLabel, InMemoryWorkflowDefinitionLabelStore>()
            ;

        return configurator;
    }
}