using Elsa.Labels.Entities;
using Elsa.Labels.Implementations;
using Elsa.Labels.Options;
using Elsa.Options;
using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Common.Implementations;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Options;
using Microsoft.Extensions.DependencyInjection;

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