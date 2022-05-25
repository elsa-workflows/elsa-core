using Elsa.Labels.Entities;
using Elsa.Labels.Implementations;
using Elsa.Labels.Options;
using Elsa.Persistence.Common.Extensions;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Persistence.Options;

#pragma warning disable CS8631

namespace Elsa.Labels.Extensions;

public static class DependencyInjectionExtensions
{
    public static ElsaOptionsConfigurator AddLabels(this ElsaOptionsConfigurator configurator, Action<LabelPersistenceOptions>? configure = default)
    {
        var services = configurator.Services;

        services
            .AddMemoryStore<Label, InMemoryLabelStore>()
            .AddMemoryStore<WorkflowDefinitionLabel, InMemoryWorkflowDefinitionLabelStore>()
            ;

        configurator.Configure(() => new LabelPersistenceOptions(configurator), configure);
        return configurator;
    }
}